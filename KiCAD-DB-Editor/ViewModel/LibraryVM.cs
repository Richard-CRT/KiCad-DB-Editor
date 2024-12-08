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

        private string _newKiCADSymbolLibraryName = "";
        public string NewKiCADSymbolLibraryName
        {
            get { return _newKiCADSymbolLibraryName; }
            set
            {
                if (_newKiCADSymbolLibraryName != value)
                {
                    _newKiCADSymbolLibraryName = value;
                    InvokePropertyChanged();
                }
            }
        }

        private string _newKiCADSymbolLibraryRelativePath = "";
        public string NewKiCADSymbolLibraryRelativePath
        {
            get { return _newKiCADSymbolLibraryRelativePath; }
            set
            {
                if (_newKiCADSymbolLibraryRelativePath != value)
                {
                    _newKiCADSymbolLibraryRelativePath = value;
                    InvokePropertyChanged();
                }
            }
        }

        private KiCADSymbolLibraryVM? _selectedKiCADSymbolLibraryVM = null;
        public KiCADSymbolLibraryVM? SelectedKiCADSymbolLibraryVM
        {
            get { return _selectedKiCADSymbolLibraryVM; }
            set
            {
                if (_selectedKiCADSymbolLibraryVM != value)
                {
                    _selectedKiCADSymbolLibraryVM = value;
                    InvokePropertyChanged();

                    if (SelectedKiCADSymbolLibraryVM is not null)
                    {
                        NewKiCADSymbolLibraryName = SelectedKiCADSymbolLibraryVM.Nickname;
                        NewKiCADSymbolLibraryRelativePath = SelectedKiCADSymbolLibraryVM.RelativePath;
                    }
                }
            }
        }

        // Do not initialise here, do in constructor to link collection changed
        private ObservableCollectionEx<KiCADFootprintLibraryVM> _kiCADFootprintLibraryVMs;
        public ObservableCollectionEx<KiCADFootprintLibraryVM> KiCADFootprintLibraryVMs
        {
            get { return _kiCADFootprintLibraryVMs; }
            set
            {
                if (_kiCADFootprintLibraryVMs != value)
                {
                    if (_kiCADFootprintLibraryVMs is not null)
                        _kiCADFootprintLibraryVMs.CollectionChanged -= _kiCADFootprintLibraryVMs_CollectionChanged;
                    _kiCADFootprintLibraryVMs = value;
                    _kiCADFootprintLibraryVMs.CollectionChanged += _kiCADFootprintLibraryVMs_CollectionChanged;

                    InvokePropertyChanged(nameof(this.KiCADFootprintLibraryVMs));
                    _kiCADFootprintLibraryVMs_CollectionChanged(this, new(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        private void _kiCADFootprintLibraryVMs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Library.KiCADFootprintLibraries = new(this.KiCADFootprintLibraryVMs.Select(kSLVM => kSLVM.KiCADFootprintLibrary));
        }

        private string _newKiCADFootprintLibraryName = "";
        public string NewKiCADFootprintLibraryName
        {
            get { return _newKiCADFootprintLibraryName; }
            set
            {
                if (_newKiCADFootprintLibraryName != value)
                {
                    _newKiCADFootprintLibraryName = value;
                    InvokePropertyChanged();
                }
            }
        }

        private string _newKiCADFootprintLibraryRelativePath = "";
        public string NewKiCADFootprintLibraryRelativePath
        {
            get { return _newKiCADFootprintLibraryRelativePath; }
            set
            {
                if (_newKiCADFootprintLibraryRelativePath != value)
                {
                    _newKiCADFootprintLibraryRelativePath = value;
                    InvokePropertyChanged();
                }
            }
        }

        private KiCADFootprintLibraryVM? _selectedKiCADFootprintLibraryVM = null;
        public KiCADFootprintLibraryVM? SelectedKiCADFootprintLibraryVM
        {
            get { return _selectedKiCADFootprintLibraryVM; }
            set
            {
                if (_selectedKiCADFootprintLibraryVM != value)
                {
                    _selectedKiCADFootprintLibraryVM = value;
                    InvokePropertyChanged();

                    if (SelectedKiCADFootprintLibraryVM is not null)
                    {
                        NewKiCADFootprintLibraryName = SelectedKiCADFootprintLibraryVM.Nickname;
                        NewKiCADFootprintLibraryRelativePath = SelectedKiCADFootprintLibraryVM.RelativePath;
                    }
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
            RenameParameterCommand = new BasicCommand(RenameParameterCommandExecuted, RenameParameterCommandCanExecute);
            DeleteParameterCommand = new BasicCommand(DeleteParameterCommandExecuted, DeleteParameterCommandCanExecute);
            NewKiCADSymbolLibraryCommand = new BasicCommand(NewKiCADSymbolLibraryCommandExecuted, NewKiCADSymbolLibraryCommandCanExecute);
            UpdateKiCADSymbolLibraryCommand = new BasicCommand(UpdateKiCADSymbolLibraryCommandExecuted, UpdateKiCADSymbolLibraryCommandCanExecute);
            DeleteKiCADSymbolLibraryCommand = new BasicCommand(DeleteKiCADSymbolLibraryCommandExecuted, DeleteKiCADSymbolLibraryCommandCanExecute);
            NewKiCADFootprintLibraryCommand = new BasicCommand(NewKiCADFootprintLibraryCommandExecuted, NewKiCADFootprintLibraryCommandCanExecute);
            UpdateKiCADFootprintLibraryCommand = new BasicCommand(UpdateKiCADFootprintLibraryCommandExecuted, UpdateKiCADFootprintLibraryCommandCanExecute);
            DeleteKiCADFootprintLibraryCommand = new BasicCommand(DeleteKiCADFootprintLibraryCommandExecuted, DeleteKiCADFootprintLibraryCommandCanExecute);
            AddFootprintCommand = new BasicCommand(AddFootprintCommandExecuted, AddFootprintCommandCanExecute);
            RemoveFootprintCommand = new BasicCommand(RemoveFootprintCommandExecuted, RemoveFootprintCommandCanExecute);

            // Initialise collection with events
            // Must do PartVMs first as CategoryVMs will use it
            PartVMs = new(library.Parts.Select(p => new PartVM(this, p)));
            Debug.Assert(_partVMs is not null);
            // Must do ParameterVMs first as CategoryVMs will use it
            ParameterVMs = new(library.Parameters.Select(p => new ParameterVM(this, p)));
            Debug.Assert(_parameterVMs is not null);
            TopLevelCategoryVMs = new(library.TopLevelCategories.OrderBy(c => c.Name).Select(c => new CategoryVM(this, null, c)));
            Debug.Assert(_topLevelCategoryVMs is not null);
            KiCADSymbolLibraryVMs = new(library.KiCADSymbolLibraries.OrderBy(kSL => kSL.Nickname).Select(kSL => new KiCADSymbolLibraryVM(this, kSL)));
            Debug.Assert(_kiCADSymbolLibraryVMs is not null);
            KiCADFootprintLibraryVMs = new(library.KiCADFootprintLibraries.OrderBy(kFL => kFL.Nickname).Select(kSL => new KiCADFootprintLibraryVM(this, kSL)));
            Debug.Assert(_kiCADFootprintLibraryVMs is not null);
        }

        private bool canNewCategory(ObservableCollectionEx<CategoryVM> categoryVMCollection)
        {
            string lowerValue = this.NewCategoryName.ToLower();
            if (this.NewCategoryName.Length > 0 && lowerValue.All(c => Utilities.SafeCategoryCharacters.Contains(c)))
            {
                if (!categoryVMCollection.Any(cVM => cVM.Name.ToLower() == lowerValue))
                {
                    return true;
                }
            }
            return false;
        }

        private void newCategory(CategoryVM? parentCategoryVM, ObservableCollectionEx<CategoryVM> categoryVMCollection)
        {
            string lowerValue = this.NewCategoryName.ToLower();
            int newIndex;
            for (newIndex = 0; newIndex < categoryVMCollection.Count; newIndex++)
            {
                CategoryVM compareCategoryVM = categoryVMCollection[newIndex];
                if (compareCategoryVM.Name.CompareTo(lowerValue) > 0)
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
        public IBasicCommand AddFootprintCommand { get; }
        public IBasicCommand RemoveFootprintCommand { get; }
        public IBasicCommand NewKiCADSymbolLibraryCommand { get; }
        public IBasicCommand UpdateKiCADSymbolLibraryCommand { get; }
        public IBasicCommand DeleteKiCADSymbolLibraryCommand { get; }
        public IBasicCommand NewKiCADFootprintLibraryCommand { get; }
        public IBasicCommand UpdateKiCADFootprintLibraryCommand { get; }
        public IBasicCommand DeleteKiCADFootprintLibraryCommand { get; }

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
            string lowerValue = this.NewParameterName.ToLower();
            if (this.NewParameterName.Length > 0 && lowerValue.All(c => Utilities.SafeParameterCharacters.Contains(c)))
            {
                if (!Utilities.ReservedParameterNames.Contains(lowerValue) && Utilities.ReservedParameterNameStarts.All(s => !lowerValue.StartsWith(s)))
                {
                    if (!ParameterVMs.Any(p => p.Name.ToLower() == lowerValue))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void NewParameterCommandExecuted(object? parameter)
        {
            ParameterVMs.Add(new(this, new(this.NewParameterName)));
            this.NewParameterName = "";
        }

        private bool RenameParameterCommandCanExecute(object? parameter)
        {
            string lowerValue = this.NewParameterName.ToLower();
            if (SelectedParameterVM is not null && this.NewParameterName.Length > 0 && lowerValue.All(c => Utilities.SafeParameterCharacters.Contains(c)))
            {
                if (!Utilities.ReservedParameterNames.Contains(lowerValue) && Utilities.ReservedParameterNameStarts.All(s => !lowerValue.StartsWith(s)))
                {
                    if (!ParameterVMs.Any(p => p.Name.ToLower() == lowerValue))
                    {
                        return true;
                    }
                }
            }
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

        private bool AddFootprintCommandCanExecute(object? parameter)
        {
            return PartVM.AddFootprintCommandCanExecute(SelectedPartVMs);
        }

        private void AddFootprintCommandExecuted(object? parameter)
        {
            PartVM.AddFootprintCommandExecuted(SelectedPartVMs);
        }

        private bool RemoveFootprintCommandCanExecute(object? parameter)
        {
            return PartVM.RemoveFootprintCommandCanExecute(SelectedPartVMs);
        }

        private void RemoveFootprintCommandExecuted(object? parameter)
        {
            PartVM.RemoveFootprintCommandExecuted(SelectedPartVMs);
        }

        private bool NewKiCADSymbolLibraryCommandCanExecute(object? parameter)
        {
            if (this.NewKiCADSymbolLibraryName.Length > 0 && this.NewKiCADSymbolLibraryRelativePath.Length > 0)
                return !(KiCADSymbolLibraryVMs.Any(p => p.Nickname.ToLower() == this.NewKiCADSymbolLibraryName.ToLower()) ||
                         KiCADSymbolLibraryVMs.Any(p => p.RelativePath.ToLower() == this.NewKiCADSymbolLibraryRelativePath.ToLower()));
            else
                return false;
        }

        private void NewKiCADSymbolLibraryCommandExecuted(object? parameter)
        {
            int newIndex;
            for (newIndex = 0; newIndex < KiCADSymbolLibraryVMs.Count; newIndex++)
            {
                var compareKSL = KiCADSymbolLibraryVMs[newIndex];
                if (compareKSL.Nickname.CompareTo(this.NewKiCADSymbolLibraryName) > 0)
                    break;
            }

            KiCADSymbolLibraryVM newKiCADSymbolLibraryVM = new(this, new(this.NewKiCADSymbolLibraryName, this.NewKiCADSymbolLibraryRelativePath));
            if (newIndex == KiCADSymbolLibraryVMs.Count)
                KiCADSymbolLibraryVMs.Add(newKiCADSymbolLibraryVM);
            else
                KiCADSymbolLibraryVMs.Insert(newIndex, newKiCADSymbolLibraryVM);

            this.NewKiCADSymbolLibraryName = "";
            this.NewKiCADSymbolLibraryRelativePath = "";
        }

        private bool UpdateKiCADSymbolLibraryCommandCanExecute(object? parameter)
        {
            if (SelectedKiCADSymbolLibraryVM is not null && this.NewKiCADSymbolLibraryName.Length > 0 && this.NewKiCADSymbolLibraryRelativePath.Length > 0)
            {
                var kiCADSymbolLibraryVMsWithSameName = KiCADSymbolLibraryVMs.Where(p => p.Nickname.ToLower() == this.NewKiCADSymbolLibraryName.ToLower()).ToArray();
                var kiCADSymbolLibraryVMsWithSameRelativePath = KiCADSymbolLibraryVMs.Where(p => p.RelativePath.ToLower() == this.NewKiCADSymbolLibraryRelativePath.ToLower()).ToArray();
                return (kiCADSymbolLibraryVMsWithSameName.Length == 0 && kiCADSymbolLibraryVMsWithSameRelativePath.Length == 0) ||
                       (kiCADSymbolLibraryVMsWithSameName.Length == 1 && kiCADSymbolLibraryVMsWithSameName[0] == SelectedKiCADSymbolLibraryVM && kiCADSymbolLibraryVMsWithSameRelativePath.Length == 0) ||
                       (kiCADSymbolLibraryVMsWithSameName.Length == 0 && kiCADSymbolLibraryVMsWithSameRelativePath.Length == 1 && kiCADSymbolLibraryVMsWithSameRelativePath[0] == SelectedKiCADSymbolLibraryVM);
            }
            else
                return false;
        }

        private void UpdateKiCADSymbolLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedKiCADSymbolLibraryVM is not null);
            SelectedKiCADSymbolLibraryVM.Nickname = this.NewKiCADSymbolLibraryName;
            SelectedKiCADSymbolLibraryVM.RelativePath = this.NewKiCADSymbolLibraryRelativePath;

            int oldIndex = KiCADSymbolLibraryVMs.IndexOf(SelectedKiCADSymbolLibraryVM);
            int newIndex = 0;
            for (int i = 0; i < KiCADSymbolLibraryVMs.Count; i++)
            {
                var compareKSL = KiCADSymbolLibraryVMs[i];
                if (compareKSL != SelectedKiCADSymbolLibraryVM)
                {
                    if (compareKSL.Nickname.CompareTo(this.NewKiCADSymbolLibraryName) > 0)
                        break;
                    newIndex++;
                }
            }
            if (oldIndex != newIndex)
                KiCADSymbolLibraryVMs.Move(oldIndex, newIndex);
        }

        private bool DeleteKiCADSymbolLibraryCommandCanExecute(object? parameter)
        {
            return SelectedKiCADSymbolLibraryVM is not null;
        }

        private void DeleteKiCADSymbolLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedKiCADSymbolLibraryVM is not null);
            KiCADSymbolLibraryVMs.Remove(SelectedKiCADSymbolLibraryVM);

            SelectedKiCADSymbolLibraryVM = KiCADSymbolLibraryVMs.FirstOrDefault();
        }

        private bool NewKiCADFootprintLibraryCommandCanExecute(object? parameter)
        {
            if (this.NewKiCADFootprintLibraryName.Length > 0 && this.NewKiCADFootprintLibraryRelativePath.Length > 0)
                return !(KiCADFootprintLibraryVMs.Any(p => p.Nickname.ToLower() == this.NewKiCADFootprintLibraryName.ToLower()) ||
                         KiCADFootprintLibraryVMs.Any(p => p.RelativePath.ToLower() == this.NewKiCADFootprintLibraryRelativePath.ToLower()));
            else
                return false;
        }

        private void NewKiCADFootprintLibraryCommandExecuted(object? parameter)
        {
            int newIndex;
            for (newIndex = 0; newIndex < KiCADFootprintLibraryVMs.Count; newIndex++)
            {
                var compareKFL = KiCADFootprintLibraryVMs[newIndex];
                if (compareKFL.Nickname.CompareTo(this.NewKiCADFootprintLibraryName) > 0)
                    break;
            }

            KiCADFootprintLibraryVM newKiCADFootprintLibraryVM = new(this, new(this.NewKiCADFootprintLibraryName, this.NewKiCADFootprintLibraryRelativePath));
            if (newIndex == KiCADFootprintLibraryVMs.Count)
                KiCADFootprintLibraryVMs.Add(newKiCADFootprintLibraryVM);
            else
                KiCADFootprintLibraryVMs.Insert(newIndex, newKiCADFootprintLibraryVM);

            this.NewKiCADFootprintLibraryName = "";
            this.NewKiCADFootprintLibraryRelativePath = "";
        }

        private bool UpdateKiCADFootprintLibraryCommandCanExecute(object? parameter)
        {
            if (SelectedKiCADFootprintLibraryVM is not null && this.NewKiCADFootprintLibraryName.Length > 0 && this.NewKiCADFootprintLibraryRelativePath.Length > 0)
            {
                var kiCADFootprintLibraryVMsWithSameName = KiCADFootprintLibraryVMs.Where(p => p.Nickname.ToLower() == this.NewKiCADFootprintLibraryName.ToLower()).ToArray();
                var kiCADFootprintLibraryVMsWithSameRelativePath = KiCADFootprintLibraryVMs.Where(p => p.RelativePath.ToLower() == this.NewKiCADFootprintLibraryRelativePath.ToLower()).ToArray();
                return (kiCADFootprintLibraryVMsWithSameName.Length == 0 && kiCADFootprintLibraryVMsWithSameRelativePath.Length == 0) ||
                       (kiCADFootprintLibraryVMsWithSameName.Length == 1 && kiCADFootprintLibraryVMsWithSameName[0] == SelectedKiCADFootprintLibraryVM && kiCADFootprintLibraryVMsWithSameRelativePath.Length == 0) ||
                       (kiCADFootprintLibraryVMsWithSameName.Length == 0 && kiCADFootprintLibraryVMsWithSameRelativePath.Length == 1 && kiCADFootprintLibraryVMsWithSameRelativePath[0] == SelectedKiCADFootprintLibraryVM);
            }
            else
                return false;
        }

        private void UpdateKiCADFootprintLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedKiCADFootprintLibraryVM is not null);
            SelectedKiCADFootprintLibraryVM.Nickname = this.NewKiCADFootprintLibraryName;
            SelectedKiCADFootprintLibraryVM.RelativePath = this.NewKiCADFootprintLibraryRelativePath;

            int oldIndex = KiCADFootprintLibraryVMs.IndexOf(SelectedKiCADFootprintLibraryVM);
            int newIndex = 0;
            for (int i = 0; i < KiCADFootprintLibraryVMs.Count; i++)
            {
                var compareKFL = KiCADFootprintLibraryVMs[i];
                if (compareKFL != SelectedKiCADFootprintLibraryVM)
                {
                    if (compareKFL.Nickname.CompareTo(this.NewKiCADFootprintLibraryName) > 0)
                        break;
                    newIndex++;
                }
            }
            if (oldIndex != newIndex)
                KiCADFootprintLibraryVMs.Move(oldIndex, newIndex);
        }

        private bool DeleteKiCADFootprintLibraryCommandCanExecute(object? parameter)
        {
            return SelectedKiCADFootprintLibraryVM is not null;
        }

        private void DeleteKiCADFootprintLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedKiCADFootprintLibraryVM is not null);
            KiCADFootprintLibraryVMs.Remove(SelectedKiCADFootprintLibraryVM);

            SelectedKiCADFootprintLibraryVM = KiCADFootprintLibraryVMs.FirstOrDefault();
        }

        #endregion Commands
    }
}
