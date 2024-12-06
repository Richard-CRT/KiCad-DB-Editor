using KiCAD_DB_Editor.Commands;
using KiCAD_DB_Editor.Exceptions;
using KiCAD_DB_Editor.Model;
using KiCAD_DB_Editor.View;
using KiCAD_DB_Editor.View.Dialogs;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Navigation;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace KiCAD_DB_Editor.ViewModel
{
    public class LibraryVM : NotifyObject
    {
        public readonly Model.Library Library;

        #region Notify Properties

        public string PartUIDScheme
        {
            get { return Library.PartUIDScheme; }
            set
            {
                if (Library.PartUIDScheme != value)
                {
                    if (value.Count(c => c == '#') != Utilities.PartUIDSchemeNumberOfWildcards)
                        throw new Exceptions.ArgumentValidationException("Proposed scheme does not contain the necessary wildcard characters");

                    Library.PartUIDScheme = value;
                    InvokePropertyChanged();
                }
            }
        }

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

                    InvokePropertyChanged(nameof(this.ParameterVMs));
                    _parameters_CollectionChanged(this, new(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        private void _parameters_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Library.Parameters = new(this.ParameterVMs.Select(p => p.Parameter));

            if (TopLevelCategoryVMs is not null)
                foreach (CategoryVM tlcVM in TopLevelCategoryVMs)
                    tlcVM.InvokePropertyChanged_AvailableParameterVMs();
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

                    InvokePropertyChanged(nameof(this.TopLevelCategoryVMs));
                    _topLevelCategoryVMs_CollectionChanged(this, new(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        private void _topLevelCategoryVMs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Library.TopLevelCategories = new(this.TopLevelCategoryVMs.Select(tlcVM => tlcVM.Category));
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

                    if (SelectedParameterVM is not null)
                        NewParameterName = SelectedParameterVM.Name;
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

        // Do not initialise here, do in constructor to link collection changed
        private ObservableCollectionEx<ViewModel.PartVM> _partVMs;
        public ObservableCollectionEx<ViewModel.PartVM> PartVMs
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

                    InvokePropertyChanged(nameof(this.PartVMs));
                    _partVMs_CollectionChanged(this, new(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        private void _partVMs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Library.Parts = new(this.PartVMs.Select(p => p.Part));
        }

        // Do not initialise here, do in constructor to link collection changed
        private ObservableCollectionEx<KiCADSymbolLibraryVM> _kiCADSymbolLibraryVMs;
        public ObservableCollectionEx<KiCADSymbolLibraryVM> KiCADSymbolLibraryVMs
        {
            get { return _kiCADSymbolLibraryVMs; }
            set
            {
                if (_kiCADSymbolLibraryVMs != value)
                {
                    if (_kiCADSymbolLibraryVMs is not null)
                        _kiCADSymbolLibraryVMs.CollectionChanged -= _kiCADSymbolLibraryVMs_CollectionChanged;
                    _kiCADSymbolLibraryVMs = value;
                    _kiCADSymbolLibraryVMs.CollectionChanged += _kiCADSymbolLibraryVMs_CollectionChanged;

                    InvokePropertyChanged(nameof(this.KiCADSymbolLibraryVMs));
                    _kiCADSymbolLibraryVMs_CollectionChanged(this, new(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        private void _kiCADSymbolLibraryVMs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Library.KiCADSymbolLibraries = new(this.KiCADSymbolLibraryVMs.Select(kSLVM => kSLVM.KiCADSymbolLibrary));
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
            RenameParameterCommand = new BasicCommand(RenameParameterCommandExecuted, RenameParameterCommandCanExecute);
            DeleteParameterCommand = new BasicCommand(DeleteParameterCommandExecuted, DeleteParameterCommandCanExecute);

            // Initialise collection with events
            // Must do PartVMs first as CategoryVMs will use it
            PartVMs = new(library.Parts.Select(p => new PartVM(this, p)));
            Debug.Assert(_partVMs is not null);
            // Must do ParameterVMs first as CategoryVMs will use it
            ParameterVMs = new(library.Parameters.Select(p => new ParameterVM(this, p)));
            Debug.Assert(_parameterVMs is not null);
            TopLevelCategoryVMs = new(library.TopLevelCategories.OrderBy(c => c.Name).Select(c => new CategoryVM(this, null, c)));
            Debug.Assert(_topLevelCategoryVMs is not null);
            KiCADSymbolLibraryVMs = new(library.KiCADSymbolLibraries.Select(kSL => new KiCADSymbolLibraryVM(kSL)));
            Debug.Assert(_kiCADSymbolLibraryVMs is not null);
        }

        private bool canNewCategory(ObservableCollectionEx<CategoryVM> categoryVMCollection)
        {
            if (this.NewCategoryName.Length > 0 && this.NewParameterName.All(c => Utilities.SafeCategoryCharacters.Contains(c)))
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
        public IBasicCommand RenameParameterCommand { get; }
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
            if (this.NewParameterName.Length > 0 && this.NewParameterName.All(c => Utilities.SafeParameterCharacters.Contains(c)))
                return !ParameterVMs.Any(p => p.Name.ToLower() == this.NewParameterName.ToLower());
            else
                return false;
        }

        private void NewParameterCommandExecuted(object? parameter)
        {
            ParameterVMs.Add(new(this, new(this.NewParameterName)));
            this.NewParameterName = "";
        }

        private bool RenameParameterCommandCanExecute(object? parameter)
        {
            if (SelectedParameterVM is not null && this.NewParameterName.Length > 0 && this.NewParameterName.All(c => Utilities.SafeParameterCharacters.Contains(c)))
                return !ParameterVMs.Any(p => p.Name.ToLower() == this.NewParameterName.ToLower());
            else
                return false;
        }

        private void RenameParameterCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedParameterVM is not null);
            SelectedParameterVM.Name = this.NewParameterName;
        }

        private bool DeleteParameterCommandCanExecute(object? parameter)
        {
            return SelectedParameterVM is not null;
        }

        private void DeleteParameterCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedParameterVM is not null);
            ParameterVMs.Remove(SelectedParameterVM);

            SelectedParameterVM = ParameterVMs.FirstOrDefault();
        }

        #endregion Commands
    }
}
