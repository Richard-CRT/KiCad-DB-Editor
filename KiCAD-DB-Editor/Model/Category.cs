using KiCAD_DB_Editor.Model.Json;
using KiCAD_DB_Editor.ViewModel;
using KiCAD_DB_Editor.ViewModel.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor.Model
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
            set
            {
                if (_name != value)
                {
                    string lowerValue = value.ToLower();
                    if (value.Length == 0 || lowerValue.Any(c => !Util.SafeCategoryCharacters.Contains(c)))
                        throw new Exceptions.ArgumentValidationException("Proposed name invalid");

                    ObservableCollectionEx<Category> categoryCollection;
                    if (ParentCategory is null)
                        categoryCollection = ParentLibrary.TopLevelCategories;
                    else
                        categoryCollection = ParentCategory.Categories;

                    if (categoryCollection.Any(cVM => cVM.Name.ToLower() == lowerValue))
                        throw new Exceptions.ArgumentValidationException("Parent already contains category with proposed name");

                    _name = value;
                    InvokePropertyChanged();

                    int oldIndex = categoryCollection.IndexOf(this);
                    int newIndex = 0;
                    for (int i = 0; i < categoryCollection.Count; i++)
                    {
                        Category compareCategory = categoryCollection[i];
                        if (compareCategory != this)
                        {
                            if (compareCategory.Name.CompareTo(this.Name) > 0)
                                break;
                            newIndex++;
                        }
                    }
                    if (oldIndex != newIndex)
                        categoryCollection.Move(oldIndex, newIndex);
                }
            }
        }

        // No setter, to prevent the VM needing to listening PropertyChanged events
        private ObservableCollectionEx<Parameter> _parameters;
        public ObservableCollectionEx<Parameter> Parameters
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
        private ObservableCollectionEx<Part> _parts;
        public ObservableCollectionEx<Part> Parts
        {
            get { return _parts; }
        }

        public ObservableCollectionEx<Parameter> InheritedAndNormalParameters
        {
            get { return new(Parameters.Concat(InheritedParameters)); }
        }

        public ObservableCollectionEx<Parameter> InheritedParameters
        {
            get { return ParentCategory is null ? new() : new(ParentCategory.InheritedAndNormalParameters); }
        }

        public ObservableCollectionEx<Parameter> AvailableParameters
        {
            get { return new(ParentLibrary.AllParameters.Except(Parameters)); }
        }

        #endregion Notify Properties

        public Category(JsonCategory jsonCategory, Library parentLibrary, Category? parentCategory)
        {
            _parentLibrary = parentLibrary;
            _parentLibrary.AllParameters.CollectionChanged += ParentLibrary_AllParameters_CollectionChanged;
            _parentCategory = parentCategory;
            if (_parentCategory is not null)
                _parentCategory.InheritedAndNormalParameters.CollectionChanged += ParentCategory_InheritedAndNormalParameters_CollectionChanged;
            Name = jsonCategory.Name;

            // Initialise collection with events
            _categories = new(jsonCategory.Categories.Select(jC => new Category(jC, ParentLibrary, this)));
            _parameters = new(ParentLibrary.AllParameters.Where(p => jsonCategory.Parameters.Contains(p.UUID)));
            _parameters.CollectionChanged += Parameters_CollectionChanged;
            _parts = new();
        }

        public Category(string name, Library parentLibrary, Category? parentCategory)
        {
            _parentLibrary = parentLibrary;
            _parentLibrary.AllParameters.CollectionChanged += ParentLibrary_AllParameters_CollectionChanged;
            _parentCategory = parentCategory;
            Name = name;

            // Initialise collection with events
            _categories = new();
            _parameters = new();
            _parameters.CollectionChanged += Parameters_CollectionChanged;
            _parts = new();
        }

        private void ParentLibrary_AllParameters_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            InvokePropertyChanged(nameof(AvailableParameters));

            foreach (Part part in Parts)
            {
                var parametersToBeRemoved = part.ParameterValues.Keys.Except(ParentLibrary.AllParameters).ToArray();
                foreach (Parameter parameterToBeRemoved in parametersToBeRemoved)
                    part.ParameterValues.Remove(parameterToBeRemoved);
            }
        }

        private void ParentCategory_InheritedAndNormalParameters_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            InvokePropertyChanged(nameof(InheritedParameters));
            InvokePropertyChanged(nameof(InheritedAndNormalParameters));

            foreach (Part part in Parts)
            {
                var parametersToBeRemoved = part.ParameterValues.Keys.Except(InheritedAndNormalParameters).ToArray();
                foreach (Parameter parameterToBeRemoved in parametersToBeRemoved)
                    part.ParameterValues.Remove(parameterToBeRemoved);
                var parametersToBeAdded = InheritedAndNormalParameters.Except(part.ParameterValues.Keys).ToArray();
                foreach (Parameter parameterToBeAdded in parametersToBeAdded)
                    part.ParameterValues.Add(parameterToBeAdded, "");
            }
        }

        private void Parameters_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            InvokePropertyChanged(nameof(AvailableParameters));
            InvokePropertyChanged(nameof(InheritedAndNormalParameters));

            foreach (Part part in Parts)
            {
                var parametersToBeRemoved = part.ParameterValues.Keys.Except(InheritedAndNormalParameters).ToArray();
                foreach (Parameter parameterToBeRemoved in parametersToBeRemoved)
                    part.ParameterValues.Remove(parameterToBeRemoved);
                var parametersToBeAdded = InheritedAndNormalParameters.Except(part.ParameterValues.Keys).ToArray();
                foreach (Parameter parameterToBeAdded in parametersToBeAdded)
                    part.ParameterValues.Add(parameterToBeAdded, "");
            }
        }
    }
}
