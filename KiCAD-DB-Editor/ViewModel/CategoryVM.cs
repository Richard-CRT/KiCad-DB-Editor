using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Net.Http.Headers;
using System.Windows.Data;
using System.ComponentModel;
using Microsoft.VisualBasic;
using KiCAD_DB_Editor.Commands;
using KiCAD_DB_Editor.View;
using KiCAD_DB_Editor.Exceptions;
using KiCAD_DB_Editor.Model;
using KiCAD_DB_Editor.View.Dialogs;
using System.Security.Cryptography;
using System.Windows.Media;
using System.Formats.Asn1;

namespace KiCAD_DB_Editor.ViewModel
{
    public class CategoryVM : NotifyObject
    {
        public readonly Model.Category Category;
        public readonly LibraryVM ParentLibraryVM;
        public readonly CategoryVM? ParentCategoryVM;

        #region Notify Properties

        public string Name
        {
            get { return Category.Name; }
            set
            {
                if (Category.Name != value)
                {
                    if (value.Length == 0 || value.Any(c => !Utilities.SafeCategoryCharacters.Contains(c)))
                        throw new Exceptions.ArgumentValidationException("Proposed name invalid");

                    ObservableCollectionEx<CategoryVM> categoryCollection;
                    if (ParentCategoryVM is null)
                        categoryCollection = ParentLibraryVM.TopLevelCategoryVMs;
                    else
                        categoryCollection = ParentCategoryVM.CategoryVMs;

                    if (categoryCollection.Any(cVM => cVM.Name.ToLower() == value.ToLower()))
                        throw new Exceptions.ArgumentValidationException("Parent already contains category with proposed name");

                    Category.Name = value;
                    InvokePropertyChanged();

                    int oldIndex = categoryCollection.IndexOf(this);
                    int newIndex = 0;
                    for (int i = 0; i < categoryCollection.Count; i++)
                    {
                        CategoryVM compareCategoryVM = categoryCollection[i];
                        if (compareCategoryVM != this)
                        {
                            if (compareCategoryVM.Name.CompareTo(this.Name) > 0)
                                break;
                            newIndex++;
                        }
                    }
                    if (oldIndex != newIndex)
                        categoryCollection.Move(oldIndex, newIndex);
                }
            }
        }

        // Do not initialise here, do in constructor to link collection changed
        private ObservableCollectionEx<CategoryVM> _categoryVMs;
        public ObservableCollectionEx<CategoryVM> CategoryVMs
        {
            get { return _categoryVMs; }
            set
            {
                if (_categoryVMs != value)
                {
                    if (_categoryVMs is not null)
                        _categoryVMs.CollectionChanged -= _categoryVMs_CollectionChanged;
                    _categoryVMs = value;
                    _categoryVMs.CollectionChanged += _categoryVMs_CollectionChanged;

                    InvokePropertyChanged();
                    _categoryVMs_CollectionChanged(this, new(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        private void _categoryVMs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Category.Categories = new(this.CategoryVMs.Select(cVM => cVM.Category));
        }

        // Do not initialise here, do in constructor to link collection changed
        private ObservableCollectionEx<ParameterVM> _parameterVMs;
        public ObservableCollectionEx<ParameterVM> ParameterVMs
        {
            get { return _parameterVMs; }
            set
            {
                if (_parameterVMs != value)
                {
                    if (_parameterVMs is not null)
                        _parameterVMs.CollectionChanged -= _parameterVMs_CollectionChanged;
                    _parameterVMs = value;
                    _parameterVMs.CollectionChanged += _parameterVMs_CollectionChanged;

                    InvokePropertyChanged();
                    _parameterVMs_CollectionChanged(this, new(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        private void _parameterVMs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Category.Parameters = new(this.ParameterVMs.Select(pVM => pVM.Parameter));

            InvokePropertyChanged(nameof(this.InheritedAndNormalParameterVMs));
            InvokePropertyChanged(nameof(this.AvailableParameterVMs));

            foreach (CategoryVM cVM in CategoryVMs)
                cVM.InvokePropertyChanged_InheritedParameterVMs();
        }

        // Should be ReadOnlyCollection really, but this binds to the Part grid view, so it needs to be ObservableCollectionEx
        public ObservableCollectionEx<ParameterVM> InheritedAndNormalParameterVMs
        {
            get { return new(ParentLibraryVM.ParameterVMs.Intersect(ParameterVMs.Concat(InheritedParameterVMs))); }
        }

        public ReadOnlyCollection<ParameterVM> InheritedParameterVMs
        {
            get
            {
                if (ParentCategoryVM is not null && ParentCategoryVM.ParameterVMs is not null)
                    return new(new List<ParameterVM>(ParentCategoryVM.InheritedParameterVMs.Concat(ParentCategoryVM.ParameterVMs).Distinct()));
                else
                    return new(new List<ParameterVM>());
            }
        }

        public ReadOnlyCollection<ParameterVM> AvailableParameterVMs
        {
            get { return new(new List<ParameterVM>(ParentLibraryVM.ParameterVMs.Except(ParameterVMs))); }
        }

        private ParameterVM? _selectedUnusedParameterVM = null;
        public ParameterVM? SelectedUnusedParameterVM
        {
            get { return _selectedUnusedParameterVM; }
            set { if (_selectedUnusedParameterVM != value) { _selectedUnusedParameterVM = value; InvokePropertyChanged(); } }
        }

        private ParameterVM? _selectedParameterVM = null;
        public ParameterVM? SelectedParameterVM
        {
            get { return _selectedParameterVM; }
            set { if (_selectedParameterVM != value) { _selectedParameterVM = value; InvokePropertyChanged(); } }
        }

        private ObservableCollectionEx<PartVM> _partVMs;
        public ObservableCollectionEx<PartVM> PartVMs
        {
            get { return _partVMs; }
            set
            {
                if (_partVMs != value)
                {
                    if (_partVMs is not null)
                        _partVMs.CollectionChanged -= _partVMs_CollectionChanged;
                    _partVMs = value;
                    _partVMs.CollectionChanged += _partVMs_CollectionChanged; ;

                    InvokePropertyChanged();
                    _partVMs_CollectionChanged(this, new(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        private void _partVMs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Category.Parts = new(this.PartVMs.Select(pVM => pVM.Part));
        }

        private PartVM[] _selectedPartVMs = Array.Empty<PartVM>();
        public PartVM[] SelectedPartVMs
        {
            // For some reason I can't do OneWayToSource :(
            get { return _selectedPartVMs; }
            set
            {
                if (_selectedPartVMs != value)
                {
                    _selectedPartVMs = value;
                    InvokePropertyChanged();
                }
            }
        }

        #endregion Notify Properties

        public CategoryVM(LibraryVM parentLibraryVM, CategoryVM? parentCategoryVM, Model.Category category)
        {
            ParentLibraryVM = parentLibraryVM;
            ParentCategoryVM = parentCategoryVM;

            // Link model
            Category = category;

            // Setup commands
            AddParameterCommand = new BasicCommand(AddParameterCommandExecuted, AddParameterCommandCanExecute);
            RemoveParameterCommand = new BasicCommand(RemoveParameterCommandExecuted, RemoveParameterCommandCanExecute);
            NewPartCommand = new BasicCommand(NewPartCommandExecuted, null);
            DeletePartsCommand = new BasicCommand(DeletePartCommandExecuted, DeletePartsCommandCanExecute);

            // Initialise collection with events
            CategoryVMs = new(category.Categories.OrderBy(c => c.Name).Select(c => new CategoryVM(ParentLibraryVM, this, c)));
            Debug.Assert(_categoryVMs is not null);
            // Link to parent library instances of ParameterVM (requires Library to have already set them up)
            ParameterVMs = new(parentLibraryVM.ParameterVMs.Where(pVM => category.Parameters.Contains(pVM.Parameter)));
            Debug.Assert(_parameterVMs is not null);
            // Link to parent library instances of PartVM (requires Library to have already set them up)
            PartVMs = new(parentLibraryVM.PartVMs.Where(pVM => category.Parts.Contains(pVM.Part)));
            Debug.Assert(_partVMs is not null);
        }

        public void InvokePropertyChanged_InheritedParameterVMs()
        {
            InvokePropertyChanged(nameof(this.InheritedParameterVMs));
            InvokePropertyChanged(nameof(this.InheritedAndNormalParameterVMs));

            foreach (PartVM partVM in PartVMs)
            {
                var parameterVMsToBeRemoved = partVM.ParameterVMs.Except(InheritedAndNormalParameterVMs).ToArray();
                foreach (ParameterVM parameterVMToBeRemoved in parameterVMsToBeRemoved)
                    partVM.RemoveParameterVM(parameterVMToBeRemoved);
                var parameterVMsToBeAdded = InheritedAndNormalParameterVMs.Except(partVM.ParameterVMs).ToArray();
                foreach (ParameterVM parameterVMToBeAdded in parameterVMsToBeAdded)
                    partVM.AddParameterVM(parameterVMToBeAdded);

                foreach (CategoryVM cVM in CategoryVMs)
                    cVM.InvokePropertyChanged_InheritedParameterVMs();
            }
        }

        public void InvokePropertyChanged_AvailableParameterVMs()
        {
            InvokePropertyChanged(nameof(this.AvailableParameterVMs));

            var pVMsToBeRemoved = ParameterVMs.Except(ParentLibraryVM.ParameterVMs).ToArray();
            foreach (var pVMToBeRemoved in pVMsToBeRemoved)
            {
                foreach (PartVM pVM in PartVMs)
                    pVM.RemoveParameterVM(pVMToBeRemoved);
            }
            ParameterVMs = new(ParentLibraryVM.ParameterVMs.Where(pVM => Category.Parameters.Contains(pVM.Parameter)));

            foreach (CategoryVM cVM in CategoryVMs)
                cVM.InvokePropertyChanged_AvailableParameterVMs();
        }

        #region Commands

        public IBasicCommand AddParameterCommand { get; }
        public IBasicCommand RemoveParameterCommand { get; }
        public IBasicCommand NewPartCommand { get; }
        public IBasicCommand DeletePartsCommand { get; }

        private bool AddParameterCommandCanExecute(object? parameter)
        {
            return SelectedUnusedParameterVM is not null;
        }

        private void AddParameterCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedUnusedParameterVM is not null);

            ParameterVM pVMToBeAdded = SelectedUnusedParameterVM;

            int indexOfPVMToBeAddedInLibrary = ParentLibraryVM.ParameterVMs.IndexOf(pVMToBeAdded);
            int newIndex;
            for (newIndex = 0; newIndex < ParameterVMs.Count; newIndex++)
            {
                if (indexOfPVMToBeAddedInLibrary < ParentLibraryVM.ParameterVMs.IndexOf(ParameterVMs[newIndex]))
                {
                    break;
                }
            }
            if (newIndex == ParameterVMs.Count)
                ParameterVMs.Add(pVMToBeAdded);
            else
                ParameterVMs.Insert(newIndex, pVMToBeAdded);

            foreach (PartVM pVM in PartVMs)
                pVM.AddParameterVM(pVMToBeAdded);

            SelectedUnusedParameterVM = AvailableParameterVMs.FirstOrDefault();
        }

        private bool RemoveParameterCommandCanExecute(object? parameter)
        {
            return SelectedParameterVM is not null;
        }

        private void RemoveParameterCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedParameterVM is not null);

            ParameterVM pVMToBeRemoved = SelectedParameterVM;

            this.ParameterVMs.Remove(pVMToBeRemoved);

            if (!InheritedParameterVMs.Contains(pVMToBeRemoved))
            {
                foreach (PartVM pVM in PartVMs)
                    pVM.RemoveParameterVM(pVMToBeRemoved);
            }

            SelectedParameterVM = ParameterVMs.FirstOrDefault();
        }

        private void NewPartCommandExecuted(object? parameter)
        {
            string partUID = Utilities.GeneratePartUID(ParentLibraryVM.PartUIDScheme);
            Part part = new(partUID);
            PartVM partVM = new(ParentLibraryVM, part);
            foreach (ParameterVM parameterVM in ParameterVMs)
                partVM.AddParameterVM(parameterVM);
            PartVMs.Add(partVM);
            ParentLibraryVM.PartVMs.Add(partVM);
        }

        private bool DeletePartsCommandCanExecute(object? parameter)
        {
            return SelectedPartVMs.Length > 0;
        }

        private void DeletePartCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedPartVMs.Length > 0);
            List<PartVM> pVMsToBeRemoved = new(SelectedPartVMs);
            foreach (PartVM pVMToBeRemoved in pVMsToBeRemoved)
            {
                PartVMs.Remove(pVMToBeRemoved);
                ParentLibraryVM.PartVMs.Remove(pVMToBeRemoved);
            }
        }

        #endregion Commands
    }
}
