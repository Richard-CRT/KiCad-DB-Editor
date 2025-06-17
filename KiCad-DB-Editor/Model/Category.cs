using KiCad_DB_Editor.Model.Json;
using KiCad_DB_Editor.ViewModel;
using KiCad_DB_Editor.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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

                    if (categoryCollection is not null && categoryCollection.Any(c => c.Name.ToLower() == lowerValue))
                        throw new Exceptions.ArgumentValidationException("Parent already contains category with proposed name");

                    _name = value;
                    InvokePropertyChanged();

                    if (categoryCollection is not null)
                    {
                        int oldIndex = categoryCollection.IndexOf(this);
                        if (oldIndex != -1)
                        {
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
            get { return new(ParentLibrary.AllParameters.Intersect(Parameters.Concat(InheritedParameters))); }
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
            _parentCategory = parentCategory;
            Name = jsonCategory.Name;

            _parameters = new(ParentLibrary.AllParameters.Where(p => jsonCategory.Parameters.Contains(p.UUID)));
            // We don't worry about unsubscribing because this object is the event publisher
            _parameters.CollectionChanged += Parameters_CollectionChanged;
            // _parameters must be set up first, as this line will rely on that
            _categories = new(jsonCategory.Categories.Select(jC => new Category(jC, ParentLibrary, this)));
            _parts = new();
        }

        public Category(string name, Library parentLibrary, Category? parentCategory)
        {
            _parentLibrary = parentLibrary;
            _parentCategory = parentCategory;

            Name = name;

            _parameters = new();
            _parameters.CollectionChanged += Parameters_CollectionChanged;
            _categories = new();
            _parts = new();
        }

        public void ParentLibrary_AllParameters_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // The values and order of this one may have changed, so invoke updates
            InvokePropertyChanged(nameof(AvailableParameters));

            // The order of these 2 may have changed, so invoke updates
            InvokePropertyChanged(nameof(InheritedParameters));
            InvokePropertyChanged(nameof(InheritedAndNormalParameters));

            // Potentially need to reorder Parameters
            // If there's no reordering to be done, Parameters isn't shuffled, so, Parameters_CollectionChanged is not called, so we can't rely on its InvokePropertyChange calls
            // hence the calls above may duplicate it
            int categoryParameterIndex = 0;
            for (int libraryParameterIndex = 0; libraryParameterIndex < ParentLibrary.AllParameters.Count; libraryParameterIndex++)
            {
                Parameter libraryParameter = ParentLibrary.AllParameters[libraryParameterIndex];
                int originalCategoryParameterIndex = Parameters.IndexOf(libraryParameter);
                if (originalCategoryParameterIndex != -1)
                {
                    if (categoryParameterIndex != originalCategoryParameterIndex)
                    {
                        // Paramater is later in the category list than it is in the category-subset of the Library list
                        Parameters.Move(originalCategoryParameterIndex, categoryParameterIndex);
                    }
                    categoryParameterIndex++;
                }
            }

            foreach (Part part in Parts)
            {
                var parametersToBeRemoved = part.ParameterValues.Keys.Except(ParentLibrary.AllParameters).ToArray();
                foreach (Parameter parameterToBeRemoved in parametersToBeRemoved)
                    part.ParameterValues.Remove(parameterToBeRemoved);
            }
        }

        public void ParentCategory_InheritedParameters_PropertyChanged()
        {
            InvokePropertyChanged(nameof(InheritedParameters));
            InvokePropertyChanged(nameof(InheritedAndNormalParameters));

            foreach (Category c in Categories) c.ParentCategory_InheritedParameters_PropertyChanged();

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

            foreach (Category c in Categories) c.ParentCategory_InheritedParameters_PropertyChanged();

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
