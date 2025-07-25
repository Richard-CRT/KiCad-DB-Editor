using KiCad_DB_Editor.Commands;
using KiCad_DB_Editor.Exceptions;
using KiCad_DB_Editor.Model;
using KiCad_DB_Editor.View;
using KiCad_DB_Editor.View.Dialogs;
using KiCad_DB_Editor.Utilities;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Navigation;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace KiCad_DB_Editor.ViewModel
{
    public class LibraryVM : NotifyObject
    {
        public Library Library { get; }

        #region Notify Properties

        public string PartUIDScheme
        {
            get { return Library.PartUIDScheme; }
            set
            {
                if (Library.PartUIDScheme != value)
                {
                    if (value.Count(c => c == '#') != Util.PartUIDSchemeNumberOfWildcards)
                        throw new Exceptions.ArgumentValidationException("Proposed scheme does not contain the necessary wildcard characters");

                    Library.PartUIDScheme = value;
                    InvokePropertyChanged();
                }
            }
        }

        private Parameter? _selectedParameter = null;
        public Parameter? SelectedParameter
        {
            get { return _selectedParameter; }
            set
            {
                if (_selectedParameter != value)
                {
                    _selectedParameter = value;
                    InvokePropertyChanged();

                    if (_selectedParameter is not null)
                        NewParameterName = _selectedParameter.Name;
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

        private ObservableCollectionEx<CategoryVM> _topLevelCategoryVMs;
        public ObservableCollectionEx<CategoryVM> TopLevelCategoryVMs
        {
            get { return _topLevelCategoryVMs; }
            set
            {
                if (_topLevelCategoryVMs != value)
                {
                    // Make sure TopLevelCategoryVM unsubscribes before we lose the objects
                    if (_topLevelCategoryVMs is not null) foreach (var cVM in _topLevelCategoryVMs) cVM.Unsubscribe();

                    _topLevelCategoryVMs = value;
                    InvokePropertyChanged();
                }
            }
        }

        private ObservableCollectionEx<PartVM> _allPartVMs;
        public ObservableCollectionEx<PartVM> AllPartVMs
        {
            get { return _allPartVMs; }
            set
            {
                if (_allPartVMs != value)
                {
                    // Make sure PartVM unsubscribes before we lose the objects
                    if (_allPartVMs is not null) foreach (var pVM in _allPartVMs) pVM.Unsubscribe();

                    _allPartVMs = value; InvokePropertyChanged();
                }
            }
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

        private string _newKiCadSymbolLibraryName = "";
        public string NewKiCadSymbolLibraryName
        {
            get { return _newKiCadSymbolLibraryName; }
            set
            {
                if (_newKiCadSymbolLibraryName != value)
                {
                    _newKiCadSymbolLibraryName = value;
                    InvokePropertyChanged();
                }
            }
        }

        private string _newKiCadSymbolLibraryRelativePath = "";
        public string NewKiCadSymbolLibraryRelativePath
        {
            get { return _newKiCadSymbolLibraryRelativePath; }
            set
            {
                if (_newKiCadSymbolLibraryRelativePath != value)
                {
                    _newKiCadSymbolLibraryRelativePath = value;
                    InvokePropertyChanged();
                }
            }
        }

        private KiCadSymbolLibrary? _selectedKiCadSymbolLibrary = null;
        public KiCadSymbolLibrary? SelectedKiCadSymbolLibrary
        {
            get { return _selectedKiCadSymbolLibrary; }
            set
            {
                if (_selectedKiCadSymbolLibrary != value)
                {
                    _selectedKiCadSymbolLibrary = value;
                    InvokePropertyChanged();

                    if (SelectedKiCadSymbolLibrary is not null)
                    {
                        NewKiCadSymbolLibraryName = SelectedKiCadSymbolLibrary.Nickname;
                        NewKiCadSymbolLibraryRelativePath = SelectedKiCadSymbolLibrary.RelativePath;
                    }
                }
            }
        }

        private string _newKiCadFootprintLibraryName = "";
        public string NewKiCadFootprintLibraryName
        {
            get { return _newKiCadFootprintLibraryName; }
            set
            {
                if (_newKiCadFootprintLibraryName != value)
                {
                    _newKiCadFootprintLibraryName = value;
                    InvokePropertyChanged();
                }
            }
        }

        private string _newKiCadFootprintLibraryRelativePath = "";
        public string NewKiCadFootprintLibraryRelativePath
        {
            get { return _newKiCadFootprintLibraryRelativePath; }
            set
            {
                if (_newKiCadFootprintLibraryRelativePath != value)
                {
                    _newKiCadFootprintLibraryRelativePath = value;
                    InvokePropertyChanged();
                }
            }
        }

        private KiCadFootprintLibrary? _selectedKiCadFootprintLibrary = null;
        public KiCadFootprintLibrary? SelectedKiCadFootprintLibrary
        {
            get { return _selectedKiCadFootprintLibrary; }
            set
            {
                if (_selectedKiCadFootprintLibrary != value)
                {
                    _selectedKiCadFootprintLibrary = value;
                    InvokePropertyChanged();

                    if (SelectedKiCadFootprintLibrary is not null)
                    {
                        NewKiCadFootprintLibraryName = SelectedKiCadFootprintLibrary.Nickname;
                        NewKiCadFootprintLibraryRelativePath = SelectedKiCadFootprintLibrary.RelativePath;
                    }
                }
            }
        }

        #endregion Notify Properties

        public LibraryVM(Model.Library library)
        {
            // Link model
            Library = library;

            Library.TopLevelCategories.CollectionChanged += Library_TopLevelCategories_CollectionChanged;
            TopLevelCategoryVMs = new(library.TopLevelCategories.Select(c => new CategoryVM(c)));
            Debug.Assert(_topLevelCategoryVMs is not null);

            Library.AllParts.CollectionChanged += AllParts_CollectionChanged;
            AllPartVMs = new(Library.AllParts.Select(p => new PartVM(p)));
            Debug.Assert(_allPartVMs is not null);

            // Setup commands
            ExportToKiCadCommand = new BasicCommand(ExportToKiCadCommandExecuted, null);

            NewTopLevelCategoryCommand = new BasicCommand(NewTopLevelCategoryCommandExecuted, NewTopLevelCategoryCommandCanExecute);
            NewSubCategoryCommand = new BasicCommand(NewSubCategoryCommandExecuted, NewSubCategoryCommandCanExecute);
            DeleteCategoryCommand = new BasicCommand(DeleteCategoryCommandExecuted, DeleteCategoryCommandCanExecute);

            NewParameterCommand = new BasicCommand(NewParameterCommandExecuted, NewParameterCommandCanExecute);
            RenameParameterCommand = new BasicCommand(RenameParameterCommandExecuted, RenameParameterCommandCanExecute);
            DeleteParameterCommand = new BasicCommand(DeleteParameterCommandExecuted, DeleteParameterCommandCanExecute);
            MoveParameterUpCommand = new BasicCommand(MoveParameterUpCommandExecuted, MoveParameterUpCommandCanExecute);
            MoveParameterDownCommand = new BasicCommand(MoveParameterDownCommandExecuted, MoveParameterDownCommandCanExecute);

            AddFootprintCommand = new BasicCommand(AddFootprintCommandExecuted, AddFootprintCommandCanExecute);
            RemoveFootprintCommand = new BasicCommand(RemoveFootprintCommandExecuted, RemoveFootprintCommandCanExecute);

            BrowseKiCadSymbolLibraryCommand = new BasicCommand(BrowseKiCadSymbolLibraryCommandExecuted, BrowseKiCadSymbolLibraryCommandCanExecute);
            NewKiCadSymbolLibraryCommand = new BasicCommand(NewKiCadSymbolLibraryCommandExecuted, NewKiCadSymbolLibraryCommandCanExecute);
            UpdateKiCadSymbolLibraryCommand = new BasicCommand(UpdateKiCadSymbolLibraryCommandExecuted, UpdateKiCadSymbolLibraryCommandCanExecute);
            DeleteKiCadSymbolLibraryCommand = new BasicCommand(DeleteKiCadSymbolLibraryCommandExecuted, DeleteKiCadSymbolLibraryCommandCanExecute);
            ReparseKiCadSymbolNamesCommand = new BasicCommand(ReparseKiCadSymbolNamesCommandExecuted, ReparseKiCadSymbolNamesCommandCanExecute);

            BrowseKiCadFootprintLibraryCommand = new BasicCommand(BrowseKiCadFootprintLibraryCommandExecuted, BrowseKiCadFootprintLibraryCommandCanExecute);
            NewKiCadFootprintLibraryCommand = new BasicCommand(NewKiCadFootprintLibraryCommandExecuted, NewKiCadFootprintLibraryCommandCanExecute);
            UpdateKiCadFootprintLibraryCommand = new BasicCommand(UpdateKiCadFootprintLibraryCommandExecuted, UpdateKiCadFootprintLibraryCommandCanExecute);
            DeleteKiCadFootprintLibraryCommand = new BasicCommand(DeleteKiCadFootprintLibraryCommandExecuted, DeleteKiCadFootprintLibraryCommandCanExecute);
            ReparseKiCadFootprintNamesCommand = new BasicCommand(ReparseKiCadFootprintNamesCommandExecuted, ReparseKiCadFootprintNamesCommandCanExecute);

        }

        public void Unsubscribe()
        {
            Library.TopLevelCategories.CollectionChanged -= Library_TopLevelCategories_CollectionChanged;
            Library.AllParts.CollectionChanged -= AllParts_CollectionChanged;
            foreach (var cVM in TopLevelCategoryVMs) cVM.Unsubscribe();
            foreach (var pVM in AllPartVMs) pVM.Unsubscribe();
        }

        private void AllParts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                // Handle some of these more precisely than Reset for efficiency
                case NotifyCollectionChangedAction.Add:
                    Debug.Assert(e.NewItems is not null && e.NewItems.Count == 1);
                    Part newPart = (e.NewItems[0] as Part)!;
                    AllPartVMs.Insert(e.NewStartingIndex, new(newPart));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    AllPartVMs.RemoveAt(e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Reset:
                default:
                    AllPartVMs = new(Library.AllParts.Select(p => new PartVM(p)));
                    break;
            }
        }

        private void Library_TopLevelCategories_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // If we reset the list the TreeView selection is lost, so we need to handle proper collection changed events
            // At least .Move, but we may as well do more
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Move:
                    // Rely on the indexes being the same as the source Categories list
                    TopLevelCategoryVMs.Move(e.OldStartingIndex, e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    // Rely on the indexes being the same as the source Categories list
                    var cVMToRemove = TopLevelCategoryVMs[e.OldStartingIndex];
                    cVMToRemove.Unsubscribe();
                    TopLevelCategoryVMs.RemoveAt(e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Add:
                    // Rely on the indexes being the same as the source Categories list
                    Debug.Assert(e.NewItems is not null && e.NewItems.Count == 1);
                    var newCategory = (e.NewItems[0] as Category)!;
                    var cVMToAdd = new CategoryVM(newCategory);
                    TopLevelCategoryVMs.Insert(e.NewStartingIndex, cVMToAdd);
                    break;
                default:
                    TopLevelCategoryVMs = new(Library.TopLevelCategories.Select(c => new CategoryVM(c)));
                    break;
            }
        }

        private bool canNewCategory(ObservableCollectionEx<Category> categoryCollection)
        {
            string lowerValue = this.NewCategoryName.ToLower();
            if (this.NewCategoryName.Length > 0 && lowerValue.All(c => Util.SafeCategoryCharacters.Contains(c)))
            {
                if (!categoryCollection.Any(c => c.Name.ToLower() == lowerValue))
                {
                    return true;
                }
            }
            return false;
        }

        private void newCategory(Category? parentCategory, ObservableCollectionEx<Category> categoryCollection)
        {
            string lowerValue = this.NewCategoryName.ToLower();
            int newIndex;
            for (newIndex = 0; newIndex < categoryCollection.Count; newIndex++)
            {
                Category compareCategory = categoryCollection[newIndex];
                if (compareCategory.Name.CompareTo(lowerValue) > 0)
                    break;
            }
            Category newCategory = new Category(this.NewCategoryName, Library, parentCategory);
            if (newIndex == categoryCollection.Count)
                categoryCollection.Add(newCategory);
            else
                categoryCollection.Insert(newIndex, newCategory);
            Library.AllCategories.Add(newCategory);
        }

        #region Commands

        public IBasicCommand ExportToKiCadCommand { get; }
        public IBasicCommand NewTopLevelCategoryCommand { get; }
        public IBasicCommand NewSubCategoryCommand { get; }
        public IBasicCommand DeleteCategoryCommand { get; }
        public IBasicCommand NewParameterCommand { get; }
        public IBasicCommand RenameParameterCommand { get; }
        public IBasicCommand DeleteParameterCommand { get; }
        public IBasicCommand MoveParameterUpCommand { get; }
        public IBasicCommand MoveParameterDownCommand { get; }
        public IBasicCommand AddFootprintCommand { get; }
        public IBasicCommand RemoveFootprintCommand { get; }
        public IBasicCommand BrowseKiCadSymbolLibraryCommand { get; }
        public IBasicCommand NewKiCadSymbolLibraryCommand { get; }
        public IBasicCommand UpdateKiCadSymbolLibraryCommand { get; }
        public IBasicCommand DeleteKiCadSymbolLibraryCommand { get; }
        public IBasicCommand ReparseKiCadSymbolNamesCommand { get; }
        public IBasicCommand BrowseKiCadFootprintLibraryCommand { get; }
        public IBasicCommand NewKiCadFootprintLibraryCommand { get; }
        public IBasicCommand UpdateKiCadFootprintLibraryCommand { get; }
        public IBasicCommand DeleteKiCadFootprintLibraryCommand { get; }
        public IBasicCommand ReparseKiCadFootprintNamesCommand { get; }

        private void ExportToKiCadCommandExecuted(object? parameter)
        {
            // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
            SaveFileDialog saveFileDialog = new();
            // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
            saveFileDialog.Title = "Export KiCad DB & Config File";
            saveFileDialog.Filter = "Project file (*.kicad_dbl)|*.kicad_dbl|All files (*.*)|*.*";
            if (saveFileDialog.ShowDialog() == true)
            {
                if (!this.Library.ExportToKiCad(false, saveFileDialog.FileName))
                {
                    // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
                    (new View.Dialogs.Window_ErrorDialog("Export failed!")).ShowDialog();
                    // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
                }
            }
        }

        private bool NewTopLevelCategoryCommandCanExecute(object? parameter)
        {
            return canNewCategory(Library.TopLevelCategories);
        }

        private void NewTopLevelCategoryCommandExecuted(object? parameter)
        {
            newCategory(null, Library.TopLevelCategories);
        }

        private bool NewSubCategoryCommandCanExecute(object? parameter)
        {
            if (parameter is not null && parameter is CategoryVM cVM)
                return canNewCategory(cVM.Category.Categories);
            else
                return false;
        }

        private void NewSubCategoryCommandExecuted(object? parameter)
        {
            CategoryVM selectedCategoryVM = (CategoryVM)parameter!;
            newCategory(selectedCategoryVM.Category, selectedCategoryVM.Category.Categories);
        }

        private bool DeleteCategoryCommandCanExecute(object? parameter)
        {
            return parameter is not null && parameter is CategoryVM cVM;
        }

        private void DeleteCategoryCommandExecuted(object? parameter)
        {
            CategoryVM selectedCategoryVM = (CategoryVM)parameter!;
            Category selectedCategory = selectedCategoryVM.Category;
            Library.AllCategories.Remove(selectedCategory);
            if (selectedCategory.ParentCategory is null)
                Library.TopLevelCategories.Remove(selectedCategory);
            else
                selectedCategory.ParentCategory.Categories.Remove(selectedCategory);
        }

        private bool NewParameterCommandCanExecute(object? parameter)
        {
            string lowerValue = this.NewParameterName.ToLower();
            if (this.NewParameterName.Length > 0 && lowerValue.All(c => Util.SafeParameterCharacters.Contains(c)))
            {
                if (!Util.ReservedParameterNames.Contains(lowerValue) && Util.ReservedParameterNameStarts.All(s => !lowerValue.StartsWith(s)))
                {
                    if (!Library.AllParameters.Any(p => p.Name.ToLower() == lowerValue))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void NewParameterCommandExecuted(object? parameter)
        {
            Parameter newParameter = new(this.NewParameterName);
            Library.AllParameters.Add(newParameter);
            SelectedParameter = newParameter;
        }

        private bool RenameParameterCommandCanExecute(object? parameter)
        {
            string lowerValue = this.NewParameterName.ToLower();
            if (SelectedParameter is not null && this.NewParameterName.Length > 0 && lowerValue.All(c => Util.SafeParameterCharacters.Contains(c)))
            {
                if (!Util.ReservedParameterNames.Contains(lowerValue) && Util.ReservedParameterNameStarts.All(s => !lowerValue.StartsWith(s)))
                {
                    if (!Library.AllParameters.Any(p => p.Name.ToLower() == lowerValue))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void RenameParameterCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedParameter is not null);
            SelectedParameter.Name = this.NewParameterName;
        }

        private bool DeleteParameterCommandCanExecute(object? parameter)
        {
            return SelectedParameter is not null;
        }

        private void DeleteParameterCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedParameter is not null);
            Library.AllParameters.Remove(SelectedParameter);

            SelectedParameter = Library.AllParameters.FirstOrDefault();
        }

        private bool MoveParameterUpCommandCanExecute(object? parameter)
        {
            return SelectedParameter is not null && Library.AllParameters.First() != SelectedParameter;
        }

        private void MoveParameterUpCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedParameter is not null);
            int oldIndex = Library.AllParameters.IndexOf(SelectedParameter);
            Debug.Assert(oldIndex > 0);
            Library.AllParameters.Move(oldIndex, oldIndex - 1);
        }

        private bool MoveParameterDownCommandCanExecute(object? parameter)
        {
            return SelectedParameter is not null && Library.AllParameters.Last() != SelectedParameter;
        }

        private void MoveParameterDownCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedParameter is not null);
            int oldIndex = Library.AllParameters.IndexOf(SelectedParameter);
            Debug.Assert(oldIndex < Library.AllParameters.Count - 1);
            Library.AllParameters.Move(oldIndex, oldIndex + 1);
        }

        private bool AddFootprintCommandCanExecute(object? parameter)
        {
            return SelectedPartVMs.Count() > 0;
        }

        private void AddFootprintCommandExecuted(object? parameter)
        {
            foreach (PartVM pVM in SelectedPartVMs)
            {
                pVM.Part.FootprintPairs.Add(("", ""));
            }
        }

        private bool RemoveFootprintCommandCanExecute(object? parameter)
        {
            return SelectedPartVMs.Count() > 0 && SelectedPartVMs.All(pVM => pVM.FootprintCount > 0);
        }

        private void RemoveFootprintCommandExecuted(object? parameter)
        {
            foreach (PartVM pVM in SelectedPartVMs)
            {
                pVM.Part.FootprintPairs.RemoveAt(pVM.Part.FootprintPairs.Count - 1);
            }
        }

        private bool BrowseKiCadSymbolLibraryCommandCanExecute(object? parameter)
        {
            return Library.ProjectDirectoryPath != "";
        }

        private void BrowseKiCadSymbolLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(Library.ProjectDirectoryPath != "");

            // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
            OpenFileDialog openFileDialog = new();
            // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
            openFileDialog.Title = "Open KiCad Symbol Library File";
            openFileDialog.Filter = "Symbol library file (*.kiCad_sym)|*.kiCad_sym|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
                this.NewKiCadSymbolLibraryRelativePath = Path.GetRelativePath(Library.ProjectDirectoryPath, openFileDialog.FileName);
        }

        private bool NewKiCadSymbolLibraryCommandCanExecute(object? parameter)
        {
            if (this.NewKiCadSymbolLibraryName.Length > 0 && this.NewKiCadSymbolLibraryRelativePath.Length > 0)
                return !Library.KiCadSymbolLibraries.Any(kSL => kSL.Nickname.ToLower() == this.NewKiCadSymbolLibraryName.ToLower());
            else
                return false;
        }

        private void NewKiCadSymbolLibraryCommandExecuted(object? parameter)
        {
            int newIndex;
            for (newIndex = 0; newIndex < Library.KiCadSymbolLibraries.Count; newIndex++)
            {
                var compareKSL = Library.KiCadSymbolLibraries[newIndex];
                if (compareKSL.Nickname.CompareTo(this.NewKiCadSymbolLibraryName) > 0)
                    break;
            }

            KiCadSymbolLibrary newKiCadSymbolLibrary = new(this.NewKiCadSymbolLibraryName, this.NewKiCadSymbolLibraryRelativePath, Library);
            if (newIndex == Library.KiCadSymbolLibraries.Count)
                Library.KiCadSymbolLibraries.Add(newKiCadSymbolLibrary);
            else
                Library.KiCadSymbolLibraries.Insert(newIndex, newKiCadSymbolLibrary);

            SelectedKiCadSymbolLibrary = newKiCadSymbolLibrary;
        }

        private bool UpdateKiCadSymbolLibraryCommandCanExecute(object? parameter)
        {
            if (SelectedKiCadSymbolLibrary is not null && this.NewKiCadSymbolLibraryName.Length > 0 && this.NewKiCadSymbolLibraryRelativePath.Length > 0)
            {
                var kiCadSymbolLibrariesWithSameName = Library.KiCadSymbolLibraries.Where(p => p.Nickname.ToLower() == this.NewKiCadSymbolLibraryName.ToLower()).ToArray();

                // Allow updates if none share the same name, or the name is the same as current, but the path is not (path case sensitive because of UNIX)
                return (kiCadSymbolLibrariesWithSameName.Length == 0) ||
                       (kiCadSymbolLibrariesWithSameName.Length == 1 && kiCadSymbolLibrariesWithSameName[0] == SelectedKiCadSymbolLibrary &&
                       this.NewKiCadSymbolLibraryRelativePath != SelectedKiCadSymbolLibrary.RelativePath);
            }
            else
                return false;
        }

        private void UpdateKiCadSymbolLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedKiCadSymbolLibrary is not null);
            SelectedKiCadSymbolLibrary.Nickname = this.NewKiCadSymbolLibraryName;
            SelectedKiCadSymbolLibrary.RelativePath = this.NewKiCadSymbolLibraryRelativePath;

            int oldIndex = Library.KiCadSymbolLibraries.IndexOf(SelectedKiCadSymbolLibrary);
            int newIndex = 0;
            for (int i = 0; i < Library.KiCadSymbolLibraries.Count; i++)
            {
                var compareKSL = Library.KiCadSymbolLibraries[i];
                if (compareKSL != SelectedKiCadSymbolLibrary)
                {
                    if (compareKSL.Nickname.CompareTo(this.NewKiCadSymbolLibraryName) > 0)
                        break;
                    newIndex++;
                }
            }
            if (oldIndex != newIndex)
                Library.KiCadSymbolLibraries.Move(oldIndex, newIndex);
        }

        private bool DeleteKiCadSymbolLibraryCommandCanExecute(object? parameter)
        {
            return SelectedKiCadSymbolLibrary is not null;
        }

        private void DeleteKiCadSymbolLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedKiCadSymbolLibrary is not null);
            Library.KiCadSymbolLibraries.Remove(SelectedKiCadSymbolLibrary);

            SelectedKiCadSymbolLibrary = Library.KiCadSymbolLibraries.FirstOrDefault();
        }

        private bool ReparseKiCadSymbolNamesCommandCanExecute(object? parameter)
        {
            return SelectedKiCadSymbolLibrary is not null;
        }

        private void ReparseKiCadSymbolNamesCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedKiCadSymbolLibrary is not null);
            SelectedKiCadSymbolLibrary.ParseKiCadSymbolNames();
        }

        private bool BrowseKiCadFootprintLibraryCommandCanExecute(object? parameter)
        {
            return Library.ProjectDirectoryPath != "";
        }

        private void BrowseKiCadFootprintLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(Library.ProjectDirectoryPath != "");

            // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
            OpenFolderDialog openFolderDialog = new();
            // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
            openFolderDialog.Title = "Open KiCad Footprint Library Directory";
            if (openFolderDialog.ShowDialog() == true)
                this.NewKiCadFootprintLibraryRelativePath = Path.GetRelativePath(Library.ProjectDirectoryPath, openFolderDialog.FolderName);
        }

        private bool NewKiCadFootprintLibraryCommandCanExecute(object? parameter)
        {
            if (this.NewKiCadFootprintLibraryName.Length > 0 && this.NewKiCadFootprintLibraryRelativePath.Length > 0)
                return !Library.KiCadFootprintLibraries.Any(p => p.Nickname.ToLower() == this.NewKiCadFootprintLibraryName.ToLower());
            else
                return false;
        }

        private void NewKiCadFootprintLibraryCommandExecuted(object? parameter)
        {
            int newIndex;
            for (newIndex = 0; newIndex < Library.KiCadFootprintLibraries.Count; newIndex++)
            {
                var compareKFL = Library.KiCadFootprintLibraries[newIndex];
                if (compareKFL.Nickname.CompareTo(this.NewKiCadFootprintLibraryName) > 0)
                    break;
            }

            KiCadFootprintLibrary newKiCadFootprintLibrary = new(this.NewKiCadFootprintLibraryName, this.NewKiCadFootprintLibraryRelativePath, Library);
            if (newIndex == Library.KiCadFootprintLibraries.Count)
                Library.KiCadFootprintLibraries.Add(newKiCadFootprintLibrary);
            else
                Library.KiCadFootprintLibraries.Insert(newIndex, newKiCadFootprintLibrary);

            SelectedKiCadFootprintLibrary = newKiCadFootprintLibrary;
        }

        private bool UpdateKiCadFootprintLibraryCommandCanExecute(object? parameter)
        {
            if (SelectedKiCadFootprintLibrary is not null && this.NewKiCadFootprintLibraryName.Length > 0 && this.NewKiCadFootprintLibraryRelativePath.Length > 0)
            {
                var kiCadFootprintLibrariesWithSameName = Library.KiCadFootprintLibraries.Where(p => p.Nickname.ToLower() == this.NewKiCadFootprintLibraryName.ToLower()).ToArray();

                // Allow updates if none share the same name, or the name is the same as current, but the path is not (path case sensitive because of UNIX)
                return (kiCadFootprintLibrariesWithSameName.Length == 0) ||
                       (kiCadFootprintLibrariesWithSameName.Length == 1 && kiCadFootprintLibrariesWithSameName[0] == SelectedKiCadFootprintLibrary &&
                       this.NewKiCadFootprintLibraryRelativePath != SelectedKiCadFootprintLibrary.RelativePath);
            }
            else
                return false;
        }

        private void UpdateKiCadFootprintLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedKiCadFootprintLibrary is not null);
            SelectedKiCadFootprintLibrary.Nickname = this.NewKiCadFootprintLibraryName;
            SelectedKiCadFootprintLibrary.RelativePath = this.NewKiCadFootprintLibraryRelativePath;

            int oldIndex = Library.KiCadFootprintLibraries.IndexOf(SelectedKiCadFootprintLibrary);
            int newIndex = 0;
            for (int i = 0; i < Library.KiCadFootprintLibraries.Count; i++)
            {
                var compareKFL = Library.KiCadFootprintLibraries[i];
                if (compareKFL != SelectedKiCadFootprintLibrary)
                {
                    if (compareKFL.Nickname.CompareTo(this.NewKiCadFootprintLibraryName) > 0)
                        break;
                    newIndex++;
                }
            }
            if (oldIndex != newIndex)
                Library.KiCadFootprintLibraries.Move(oldIndex, newIndex);
        }

        private bool DeleteKiCadFootprintLibraryCommandCanExecute(object? parameter)
        {
            return SelectedKiCadFootprintLibrary is not null;
        }

        private void DeleteKiCadFootprintLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedKiCadFootprintLibrary is not null);
            Library.KiCadFootprintLibraries.Remove(SelectedKiCadFootprintLibrary);

            SelectedKiCadFootprintLibrary = Library.KiCadFootprintLibraries.FirstOrDefault();
        }

        private bool ReparseKiCadFootprintNamesCommandCanExecute(object? parameter)
        {
            return SelectedKiCadFootprintLibrary is not null;
        }

        private void ReparseKiCadFootprintNamesCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedKiCadFootprintLibrary is not null);
            SelectedKiCadFootprintLibrary.ParseKiCadFootprintNames();
        }

        #endregion Commands
    }
}
