using KiCAD_DB_Editor.Commands;
using KiCAD_DB_Editor.Exceptions;
using KiCAD_DB_Editor.Model;
using KiCAD_DB_Editor.View;
using KiCAD_DB_Editor.View.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace KiCAD_DB_Editor.ViewModel
{
    public class LibraryVM : NotifyObject
    {
        public readonly Model.Library Library;

        public ReadOnlyCollection<ParameterVM> SpecialParameterVMs { get; } = new(new List<ParameterVM>() {
            new(new("Part UID")),
            new(new("Description")),
        });

        #region Notify Properties

        // Do not initialise here, do in constructor to link collection changed
        private ObservableCollectionEx<ViewModel.ParameterVM> _parameterVMs;
        public ObservableCollectionEx<ViewModel.ParameterVM> ParameterVMs
        {
            get { return _parameterVMs; }
            set
            {
                if (_parameterVMs != value)
                {
                    if (_parameterVMs is not null)
                        _parameterVMs.CollectionChanged -= _parameters_CollectionChanged;
                    _parameterVMs = value;
                    _parameterVMs.CollectionChanged += _parameters_CollectionChanged; ;

                    _parameters_CollectionChanged(this, new(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        private void _parameters_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Library.Parameters = new(this.ParameterVMs.Select(p => p.Parameter));

            InvokePropertyChanged(nameof(this.ParameterVMs));
        }

        // Do not initialise here, do in constructor to link collection changed
        private ObservableCollectionEx<ViewModel.CategoryVM> _topLevelCategoryVMs;
        public ObservableCollectionEx<ViewModel.CategoryVM> TopLevelCategoryVMs
        {
            get { return _topLevelCategoryVMs; }
            set
            {
                if (_topLevelCategoryVMs != value)
                {
                    if (_topLevelCategoryVMs is not null)
                        _topLevelCategoryVMs.CollectionChanged -= _topLevelCategoryVMs_CollectionChanged;
                    _topLevelCategoryVMs = value;
                    _topLevelCategoryVMs.CollectionChanged += _topLevelCategoryVMs_CollectionChanged;

                    _topLevelCategoryVMs_CollectionChanged(this, new(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        private void _topLevelCategoryVMs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Library.TopLevelCategories = new(this.TopLevelCategoryVMs.Select(tlcVM => tlcVM.Category));

            InvokePropertyChanged(nameof(this.TopLevelCategoryVMs));
        }

        private ParameterVM? _selectedParameterVM = null;
        public ParameterVM? SelectedParameterVM
        {
            get { return _selectedParameterVM; }
            set
            {
                if (_selectedParameterVM != value)
                {
                    _selectedParameterVM = value;
                    InvokePropertyChanged();
                }
            }
        }

        private string _newCategoryName = "";
        public string NewCategoryName
        {
            get { return _newCategoryName; }
            set
            {
                if (_newCategoryName != value)
                {
                    _newCategoryName = value;
                    InvokePropertyChanged();
                }
            }
        }

        private string _newParameterName = "";
        public string NewParameterName
        {
            get { return _newParameterName; }
            set
            {
                if (_newParameterName != value)
                {
                    _newParameterName = value;
                    InvokePropertyChanged();
                }
            }
        }

        #endregion Notify Properties

        public LibraryVM(Model.Library library)
        {
            // Link model
            Library = library;

            // Setup commands
            NewTopLevelCategoryCommand = new BasicCommand(NewTopLevelCategoryCommandExecuted, NewTopLevelCategoryCommandCanExecute);
            NewSubCategoryCommand = new BasicCommand(NewSubCategoryCommandExecuted, NewSubCategoryCommandCanExecute);
            DeleteCategoryCommand = new BasicCommand(DeleteCategoryCommandExecuted, DeleteCategoryCommandCanExecute);
            NewParameterCommand = new BasicCommand(NewParameterCommandExecuted, NewParameterCommandCanExecute);
            DeleteParameterCommand = new BasicCommand(DeleteParameterCommandExecuted, DeleteParameterCommandCanExecute);

            // Initialise collection with events
            // Must do ParameterVMs first as CategoryVMs will use it
            ParameterVMs = new(library.Parameters.OrderBy(c => c.Name).Select(p => new ParameterVM(p)));
            Debug.Assert(_parameterVMs is not null);
            TopLevelCategoryVMs = new(library.TopLevelCategories.OrderBy(c => c.Name).Select(c => new CategoryVM(this, null, c)));
            Debug.Assert(_topLevelCategoryVMs is not null);
        }

        private bool canNewCategory(ObservableCollectionEx<CategoryVM> categoryVMCollection)
        {
            if (this.NewCategoryName != "")
                return !categoryVMCollection.Any(cVM => cVM.Name.ToLower() == this.NewCategoryName.ToLower());
            else
                return false;
        }

        private void newCategory(CategoryVM? parentCategoryVM, ObservableCollectionEx<CategoryVM> categoryVMCollection)
        {
            int newIndex;
            for (newIndex = 0; newIndex < categoryVMCollection.Count; newIndex++)
            {
                CategoryVM compareCategoryVM = categoryVMCollection[newIndex];
                if (compareCategoryVM.Name.CompareTo(this.NewCategoryName.ToLower()) > 0)
                    break;
            }
            if (newIndex == categoryVMCollection.Count)
                categoryVMCollection.Add(new(this, parentCategoryVM, new(this.NewCategoryName)));
            else
                categoryVMCollection.Insert(newIndex, new(this, parentCategoryVM, new(this.NewCategoryName)));
        }

        #region Commands

        public IBasicCommand NewTopLevelCategoryCommand { get; }
        public IBasicCommand NewSubCategoryCommand { get; }
        public IBasicCommand DeleteCategoryCommand { get; }
        public IBasicCommand NewParameterCommand { get; }
        public IBasicCommand DeleteParameterCommand { get; }

        private bool NewTopLevelCategoryCommandCanExecute(object? parameter)
        {
            return canNewCategory(TopLevelCategoryVMs);
        }

        private void NewTopLevelCategoryCommandExecuted(object? parameter)
        {
            newCategory(null, TopLevelCategoryVMs);
        }

        private bool NewSubCategoryCommandCanExecute(object? parameter)
        {
            if (parameter is not null && parameter is CategoryVM cVM)
                return canNewCategory(cVM.CategoryVMs);
            else
                return false;
        }

        private void NewSubCategoryCommandExecuted(object? parameter)
        {
            CategoryVM selectedCategoryVM = (CategoryVM)parameter!;
            newCategory(selectedCategoryVM, selectedCategoryVM.CategoryVMs);
        }

        private bool DeleteCategoryCommandCanExecute(object? parameter)
        {
            return parameter is not null && parameter is CategoryVM cVM;
        }

        private void DeleteCategoryCommandExecuted(object? parameter)
        {
            CategoryVM selectedCategoryVM = (CategoryVM)parameter!;
            if (selectedCategoryVM.ParentCategoryVM is null)
                this.TopLevelCategoryVMs.Remove(selectedCategoryVM);
            else
                selectedCategoryVM.ParentCategoryVM.CategoryVMs.Remove(selectedCategoryVM);
        }

        private bool NewParameterCommandCanExecute(object? parameter)
        {
            if (this.NewParameterName != "")
                return !ParameterVMs.Any(p => p.Name.ToLower() == this.NewParameterName.ToLower());
            else
                return false;
        }

        private void NewParameterCommandExecuted(object? parameter)
        {
            int newIndex;
            for (newIndex = 0; newIndex < ParameterVMs.Count; newIndex++)
            {
                ParameterVM compareParameterVM = ParameterVMs[newIndex];
                if (compareParameterVM.Name.CompareTo(this.NewParameterName.ToLower()) > 0)
                    break;
            }
            if (newIndex == ParameterVMs.Count)
                ParameterVMs.Add(new(new(this.NewParameterName)));
            else
                ParameterVMs.Insert(newIndex, new(new(this.NewParameterName)));
        }

        private bool DeleteParameterCommandCanExecute(object? parameter)
        {
            return SelectedParameterVM is not null;
        }

        private void DeleteParameterCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedParameterVM is not null);
            ParameterVMs.Remove(SelectedParameterVM);
        }

        #endregion Commands
    }
}
