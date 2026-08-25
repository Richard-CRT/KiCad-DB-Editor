using KiCad_DB_Editor.Model.Json;
using KiCad_DB_Editor.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCad_DB_Editor.Model
{
    public class Category : NotifyObject
    {
        #region Notify Properties

        // No setter, to prevent the VM needing to listening PropertyChanged events
        private Category? _parentCategory;
        public Category? ParentCategory
        {
            get { return _parentCategory; }
        }

        // No setter, to prevent the VM needing to listening PropertyChanged events
        private Library _parentLibrary;
        public Library ParentLibrary
        {
            get { return _parentLibrary; }
        }

        private string _name = "";
        public string Name
        {
            get { return _name; }
            set { if (_name != value) { _name = value; InvokePropertyChanged(); } }
        }

        public bool IsPartUIDTemplateOverridden
        {
            get;
            set
            {
                if (field != value)
                {
                    // Must do this before updating the override as the value of ActivePartUIDTemplate depends on it
                    if (value)
                        PartUIDTemplate = ActivePartUIDTemplate;

                    field = value;

                    // Must do this after updating the override as the value of ActivePartUIDTemplate depends on it
                    if (!value)
                        PartUIDTemplate = ActivePartUIDTemplate;

                    InvokePropertyChanged();

                    InvokePropertyChanged(nameof(ActivePartUIDTemplate));
                    if (Categories is not null) // Null guard in case Categories hasn't been created yet
                        foreach (Category c in Categories) c.ParentCategory_PartUIDTemplate_PropertyChanged();
                }
            }
        } = false;

        public string PartUIDTemplate
        {
            get;
            set
            {
                if (field != value)
                {
                    if (value.Count(c => c == '#') != Util.PartUIDTemplateNumberOfWildcards)
                        throw new Exceptions.ArgumentValidationException("Proposed template does not contain the necessary wildcard characters");

                    field = value;
                    InvokePropertyChanged();

                    InvokePropertyChanged(nameof(ActivePartUIDTemplate));
                    if (Categories is not null) // Null guard in case Categories hasn't been created yet
                        foreach (Category c in Categories) c.ParentCategory_PartUIDTemplate_PropertyChanged();
                }
            }
        } = "";

        public string ActivePartUIDTemplate => IsPartUIDTemplateOverridden ? PartUIDTemplate : (ParentCategory is not null ? ParentCategory.ActivePartUIDTemplate : ParentLibrary.PartUIDTemplate);

        // No setter, to prevent the VM needing to listening PropertyChanged events
        private ObservableCollectionEx<string> _parameters;
        public ObservableCollectionEx<string> Parameters
        {
            get { return _parameters; }
        }

        // No setter, to prevent the VM needing to listening PropertyChanged events
        private ObservableCollectionEx<Category> _categories;
        public ObservableCollectionEx<Category> Categories
        {
            get { return _categories; }
        }

        // No setter, to prevent the VM needing to listening PropertyChanged events
        private ObservableCollectionEx<Category> _allSubCategories;
        public ObservableCollectionEx<Category> AllSubCategories
        {
            get { return _allSubCategories; }
        }

        // No setter, to prevent the VM needing to listening PropertyChanged events
        private ObservableCollectionEx<Part> _parts;
        public ObservableCollectionEx<Part> Parts
        {
            get { return _parts; }
        }

        public int RecursivePartsCount
        {
            get { return Parts.Count + AllSubCategories.Sum(c => c.Parts.Count); }
        }

        // Needs to be ObservableCollectionEx because bindings expecting this form
        public ObservableCollectionEx<string> InheritedAndNormalParameters
        {
            // I think .Distinct is not guaranteed to behave as I want it to i.e. keeping the first one it finds, but it is right now anyway! Hopefully it doesn't change :)
            // We want inherited parameters at the end, but want the Distinct call to prioritise keeping the Inherited one, so need some Reverses
            get { return new(Parameters.Concat(InheritedParameters).Reverse().Distinct().Reverse()); }
        }

        // Needs to be ObservableCollectionEx because bindings expecting this form
        public ObservableCollectionEx<string> InheritedParameters
        {
            get { return ParentCategory is null ? new(ParentLibrary.UniversalParameters) : new(ParentCategory.InheritedAndNormalParameters); }
        }

        #endregion Notify Properties

        public Category(JsonCategory jsonCategory, Library parentLibrary, Category? parentCategory)
        {
            _parentLibrary = parentLibrary;
            _parentCategory = parentCategory;

            Name = jsonCategory.Name;

            IsPartUIDTemplateOverridden = jsonCategory.PartUIDTemplate != "";
            PartUIDTemplate = IsPartUIDTemplateOverridden ? jsonCategory.PartUIDTemplate : ActivePartUIDTemplate;

            _parameters = new(jsonCategory.Parameters);
            // We don't worry about unsubscribing because this object is the event publisher
            _parameters.CollectionChanged += Parameters_CollectionChanged;
            _allSubCategories = new();
            // _parameters & _allSubCategories must be set up first, as this line will rely on that
            _categories = new(jsonCategory.Categories.Select(jC => new Category(jC, ParentLibrary, this)));
            _parts = new();
            // We don't worry about unsubscribing because this object is the event publisher
            _parts.CollectionChanged += Parts_CollectionChanged;
        }

        public Category(string name, Library parentLibrary, Category? parentCategory)
        {
            _parentLibrary = parentLibrary;
            _parentCategory = parentCategory;

            Name = name;

            _parameters = new();
            // We don't worry about unsubscribing because this object is the event publisher
            _parameters.CollectionChanged += Parameters_CollectionChanged;
            _allSubCategories = new();
            _categories = new();
            _parts = new();
            // We don't worry about unsubscribing because this object is the event publisher
            _parts.CollectionChanged += Parts_CollectionChanged;
        }

        public void ParentCategory_PartUIDTemplate_PropertyChanged()
        {
            if (!IsPartUIDTemplateOverridden)
                PartUIDTemplate = ActivePartUIDTemplate;

            InvokePropertyChanged(nameof(ActivePartUIDTemplate));

            foreach (Category c in Categories) c.ParentCategory_PartUIDTemplate_PropertyChanged();
        }

        public void ParentCategory_InheritedParameters_PropertyChanged(NotifyCollectionChangedEventArgs e)
        {
            InvokePropertyChanged(nameof(InheritedParameters));
            InvokePropertyChanged(nameof(InheritedAndNormalParameters));

            foreach (Category c in Categories) c.ParentCategory_InheritedParameters_PropertyChanged(e);

            UpdatePartsParameters(e);
        }

        public void ChildCategory_Parts_CollectionChanged()
        {
            InvokePropertyChanged(nameof(RecursivePartsCount));

            ParentCategory?.ChildCategory_Parts_CollectionChanged();
        }

        private void Parameters_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            InvokePropertyChanged(nameof(InheritedAndNormalParameters));

            UpdatePartsParameters(e);

            foreach (Category c in Categories) c.ParentCategory_InheritedParameters_PropertyChanged(e);

            this.ParentLibrary.Category_ParametersCollectionChanged();
        }

        private void Parts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ChildCategory_Parts_CollectionChanged();
        }

        private void UpdatePartsParameters(NotifyCollectionChangedEventArgs e)
        {
            foreach (Part part in Parts)
            {
                if (e.Action == NotifyCollectionChangedAction.Replace)
                {
                    Debug.Assert(e.OldItems!.Count == 1);
                    Debug.Assert(e.NewItems!.Count == 1);
                    string newParameterName = (string)e.NewItems[0]!;
                    string oldParameterName = (string)e.OldItems[0]!;

                    // Need to handle:
                    // The parts already have new parameter (what to do with the value in the new one?)
                    // The parts don't have new parameter (create it)
                    // The parts should still have old parameter after rename (what to do with the value in the old one?)

                    if (part.ParameterValues.ContainsKey(newParameterName))
                    {
                        // Could choose to leave the existing new parameter value if the old parameter value is blank
                        // but for now favour consistency in the data replacement
                        // if (part.ParameterValues.GetValueOrDefault(oldParameterName, "") != "")

                        // Override the existing new parameter value with the value in the old parameter
                        part.ParameterValues[newParameterName] = part.ParameterValues[oldParameterName];
                    }
                    else
                        part.ParameterValues.Add(newParameterName, part.ParameterValues.GetValueOrDefault(oldParameterName, ""));

                    if (InheritedAndNormalParameters.Contains(oldParameterName))
                        // Needs to still have the parameter after rename
                        part.ParameterValues[oldParameterName] = "";
                    else
                        part.ParameterValues.Remove(oldParameterName);
                }
                else
                {
                    var parametersToBeRemoved = part.ParameterValues.Keys.Except(InheritedAndNormalParameters).ToArray();
                    foreach (string parameterToBeRemoved in parametersToBeRemoved)
                        part.ParameterValues.Remove(parameterToBeRemoved);

                    var parametersToBeAdded = InheritedAndNormalParameters.Except(part.ParameterValues.Keys).ToArray();
                    foreach (string parameterToBeAdded in parametersToBeAdded)
                        // May already exist
                        part.ParameterValues.TryAdd(parameterToBeAdded, "");
                }
            }
        }

        public override string ToString()
        {
            return $"Category: {Name} [{Parts.Count}]";
        }
    }
}
