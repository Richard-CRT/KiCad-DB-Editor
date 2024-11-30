using KiCAD_DB_Editor.Commands;
using KiCAD_DB_Editor.Exceptions;
using KiCAD_DB_Editor.Model;
using KiCAD_DB_Editor.View;
using KiCAD_DB_Editor.View.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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

        #region Notify Properties

        private CategoryVM? _selectedCategoryVM = null;
        public CategoryVM? SelectedCategoryVM
        {
            get { return _selectedCategoryVM; }
            set
            {
                if (_selectedCategoryVM != value)
                {
                    _selectedCategoryVM = value;
                    InvokePropertyChanged();
                }
            }
        }

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

        #endregion Notify Properties

        public LibraryVM(Model.Library library)
        {
            // Link model
            Library = library;

            // Setup commands
            NewTopLevelCategoryCommand = new BasicCommand(NewTopLevelCategoryCommandExecuted, null);
            NewSubCategoryCommand = new BasicCommand(NewSubCategoryCommandExecuted, NewSubCategoryCommandCanExecute);
            DeleteCategoryCommand = new BasicCommand(DeleteCategoryCommandExecuted, DeleteCategoryCommandCanExecute);

            // Initialise collection with events
            TopLevelCategoryVMs = new(library.TopLevelCategories.OrderBy(c => c.Name).Select(c => new CategoryVM(this, null, c)));
            Debug.Assert(_topLevelCategoryVMs is not null);
        }

        private void newCategory(CategoryVM? parentCategoryVM, ObservableCollectionEx<CategoryVM> categoryCollection)
        {
            const string prefix = "New Category ";
            int newCategoryNumber = 1;
            while (categoryCollection.Any(cVM => cVM.Name.ToLower() == $"{prefix}{newCategoryNumber}".ToLower()))
                newCategoryNumber++;

            string newName = $"{prefix}{newCategoryNumber}";

            int newIndex;
            for (newIndex = 0; newIndex < categoryCollection.Count; newIndex++)
            {
                CategoryVM compareCategoryVM = categoryCollection[newIndex];
                if (compareCategoryVM.Name.CompareTo(newName.ToLower()) > 0)
                    break;
            }
            if (newIndex == categoryCollection.Count)
                categoryCollection.Add(new(this, parentCategoryVM, new(newName)));
            else
                categoryCollection.Insert(newIndex, new(this, parentCategoryVM, new(newName)));
        }

        #region Commands

        public IBasicCommand NewTopLevelCategoryCommand { get; }
        public IBasicCommand NewSubCategoryCommand { get; }
        public IBasicCommand DeleteCategoryCommand { get; }

        private void NewTopLevelCategoryCommandExecuted(object? parameter)
        {
            newCategory(null, this.TopLevelCategoryVMs);
        }

        private bool NewSubCategoryCommandCanExecute(object? parameter)
        {
            return parameter is not null && parameter is CategoryVM cVM;
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

        #endregion Commands
    }
}
