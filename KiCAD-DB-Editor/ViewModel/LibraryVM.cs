using KiCAD_DB_Editor.Commands;
using KiCAD_DB_Editor.Exceptions;
using KiCAD_DB_Editor.Model;
using KiCAD_DB_Editor.View;
using KiCAD_DB_Editor.View.Dialogs;
using KiCAD_DB_Editor.ViewModel.Utilities;
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

namespace KiCAD_DB_Editor.ViewModel
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

                    if (SelectedParameter is not null)
                        NewParameterName = SelectedParameter.Name;
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
            set { if (_topLevelCategoryVMs != value) _topLevelCategoryVMs = value; InvokePropertyChanged(); }
        }

        private ObservableCollectionEx<PartVM> _allPartVMs;
        public ObservableCollectionEx<PartVM> AllPartVMs
        {
            get { return _allPartVMs; }
            set { if (_allPartVMs != value) _allPartVMs = value; InvokePropertyChanged(); }
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

        private KiCADSymbolLibrary? _selectedKiCADSymbolLibrary = null;
        public KiCADSymbolLibrary? SelectedKiCADSymbolLibrary
        {
            get { return _selectedKiCADSymbolLibrary; }
            set
            {
                if (_selectedKiCADSymbolLibrary != value)
                {
                    _selectedKiCADSymbolLibrary = value;
                    InvokePropertyChanged();

                    if (SelectedKiCADSymbolLibrary is not null)
                    {
                        NewKiCADSymbolLibraryName = SelectedKiCADSymbolLibrary.Nickname;
                        NewKiCADSymbolLibraryRelativePath = SelectedKiCADSymbolLibrary.RelativePath;
                    }
                }
            }
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

        private KiCADFootprintLibrary? _selectedKiCADFootprintLibrary = null;
        public KiCADFootprintLibrary? SelectedKiCADFootprintLibrary
        {
            get { return _selectedKiCADFootprintLibrary; }
            set
            {
                if (_selectedKiCADFootprintLibrary != value)
                {
                    _selectedKiCADFootprintLibrary = value;
                    InvokePropertyChanged();

                    if (SelectedKiCADFootprintLibrary is not null)
                    {
                        NewKiCADFootprintLibraryName = SelectedKiCADFootprintLibrary.Nickname;
                        NewKiCADFootprintLibraryRelativePath = SelectedKiCADFootprintLibrary.RelativePath;
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
            // Make sure TopLevelCategoryVM unsubscribes before we lose the objects
            if (TopLevelCategoryVMs is not null) foreach (var cVM in TopLevelCategoryVMs) cVM.Unsubscribe();
            TopLevelCategoryVMs = new(library.TopLevelCategories.Select(c => new CategoryVM(c)));
            Debug.Assert(_topLevelCategoryVMs is not null);

            Library.AllParts.CollectionChanged += AllParts_CollectionChanged;
            // Make sure PartVM unsubscribes before we lose the objects
            if (AllPartVMs is not null) foreach (var pVM in AllPartVMs) pVM.Unsubscribe();
            AllPartVMs = new(Library.AllParts.Select(p => new PartVM(p)));
            Debug.Assert(_allPartVMs is not null);

            // Setup commands
            ExportToKiCADCommand = new BasicCommand(ExportToKiCADCommandExecuted, null);
            NewTopLevelCategoryCommand = new BasicCommand(NewTopLevelCategoryCommandExecuted, NewTopLevelCategoryCommandCanExecute);
            NewSubCategoryCommand = new BasicCommand(NewSubCategoryCommandExecuted, NewSubCategoryCommandCanExecute);
            DeleteCategoryCommand = new BasicCommand(DeleteCategoryCommandExecuted, DeleteCategoryCommandCanExecute);
            NewParameterCommand = new BasicCommand(NewParameterCommandExecuted, NewParameterCommandCanExecute);
            RenameParameterCommand = new BasicCommand(RenameParameterCommandExecuted, RenameParameterCommandCanExecute);
            DeleteParameterCommand = new BasicCommand(DeleteParameterCommandExecuted, DeleteParameterCommandCanExecute);
            BrowseKiCADSymbolLibraryCommand = new BasicCommand(BrowseKiCADSymbolLibraryCommandExecuted, BrowseKiCADSymbolLibraryCommandCanExecute);
            NewKiCADSymbolLibraryCommand = new BasicCommand(NewKiCADSymbolLibraryCommandExecuted, NewKiCADSymbolLibraryCommandCanExecute);
            UpdateKiCADSymbolLibraryCommand = new BasicCommand(UpdateKiCADSymbolLibraryCommandExecuted, UpdateKiCADSymbolLibraryCommandCanExecute);
            DeleteKiCADSymbolLibraryCommand = new BasicCommand(DeleteKiCADSymbolLibraryCommandExecuted, DeleteKiCADSymbolLibraryCommandCanExecute);
            ReparseKiCADSymbolNamesCommand = new BasicCommand(ReparseKiCADSymbolNamesCommandExecuted, ReparseKiCADSymbolNamesCommandCanExecute);
            BrowseKiCADFootprintLibraryCommand = new BasicCommand(BrowseKiCADFootprintLibraryCommandExecuted, BrowseKiCADFootprintLibraryCommandCanExecute);
            NewKiCADFootprintLibraryCommand = new BasicCommand(NewKiCADFootprintLibraryCommandExecuted, NewKiCADFootprintLibraryCommandCanExecute);
            UpdateKiCADFootprintLibraryCommand = new BasicCommand(UpdateKiCADFootprintLibraryCommandExecuted, UpdateKiCADFootprintLibraryCommandCanExecute);
            DeleteKiCADFootprintLibraryCommand = new BasicCommand(DeleteKiCADFootprintLibraryCommandExecuted, DeleteKiCADFootprintLibraryCommandCanExecute);
            ReparseKiCADFootprintNamesCommand = new BasicCommand(ReparseKiCADFootprintNamesCommandExecuted, ReparseKiCADFootprintNamesCommandCanExecute);
            AddFootprintCommand = new BasicCommand(AddFootprintCommandExecuted, AddFootprintCommandCanExecute);
            RemoveFootprintCommand = new BasicCommand(RemoveFootprintCommandExecuted, RemoveFootprintCommandCanExecute);
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
            // Make sure PartVM unsubscribes before we lose the objects
            if (AllPartVMs is not null) foreach (var pVM in AllPartVMs) pVM.Unsubscribe();
            AllPartVMs = new(Library.AllParts.Select(p => new PartVM(p)));
        }

        private void Library_TopLevelCategories_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Make sure TopLevelCategoryVM unsubscribes before we lose the objects
            if (TopLevelCategoryVMs is not null) foreach (var cVM in TopLevelCategoryVMs) cVM.Unsubscribe();
            TopLevelCategoryVMs = new(Library.TopLevelCategories.Select(c => new CategoryVM(c)));
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

        public IBasicCommand ExportToKiCADCommand { get; }
        public IBasicCommand NewTopLevelCategoryCommand { get; }
        public IBasicCommand NewSubCategoryCommand { get; }
        public IBasicCommand DeleteCategoryCommand { get; }
        public IBasicCommand NewParameterCommand { get; }
        public IBasicCommand RenameParameterCommand { get; }
        public IBasicCommand DeleteParameterCommand { get; }
        public IBasicCommand AddFootprintCommand { get; }
        public IBasicCommand RemoveFootprintCommand { get; }
        public IBasicCommand BrowseKiCADSymbolLibraryCommand { get; }
        public IBasicCommand NewKiCADSymbolLibraryCommand { get; }
        public IBasicCommand UpdateKiCADSymbolLibraryCommand { get; }
        public IBasicCommand DeleteKiCADSymbolLibraryCommand { get; }
        public IBasicCommand ReparseKiCADSymbolNamesCommand { get; }
        public IBasicCommand BrowseKiCADFootprintLibraryCommand { get; }
        public IBasicCommand NewKiCADFootprintLibraryCommand { get; }
        public IBasicCommand UpdateKiCADFootprintLibraryCommand { get; }
        public IBasicCommand DeleteKiCADFootprintLibraryCommand { get; }
        public IBasicCommand ReparseKiCADFootprintNamesCommand { get; }

        private void ExportToKiCADCommandExecuted(object? parameter)
        {
            // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
            SaveFileDialog saveFileDialog = new();
            // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
            saveFileDialog.Title = "Export KiCAD DB & Config File";
            saveFileDialog.Filter = "Project file (*.kicad_dbl)|*.kicad_dbl|All files (*.*)|*.*";
            if (saveFileDialog.ShowDialog() == true)
            {
                if (!this.Library.ExportToKiCAD(saveFileDialog.FileName))
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
            Library.AllParameters.Add(new(this.NewParameterName));
            this.NewParameterName = "";
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

        private bool AddFootprintCommandCanExecute(object? parameter)
        {
            return SelectedPartVMs.Count() > 0;
        }

        private void AddFootprintCommandExecuted(object? parameter)
        {
            foreach (PartVM pVM in SelectedPartVMs)
            {
                pVM.Part.FootprintPairs.Add(("",""));
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

        private bool BrowseKiCADSymbolLibraryCommandCanExecute(object? parameter)
        {
            return Library.ProjectDirectoryPath != "";
        }

        private void BrowseKiCADSymbolLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(Library.ProjectDirectoryPath != "");

            // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
            OpenFileDialog openFileDialog = new();
            // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
            openFileDialog.Title = "Open KiCAD Symbol Library File";
            openFileDialog.Filter = "Symbol library file (*.kicad_sym)|*.kicad_sym|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
                this.NewKiCADSymbolLibraryRelativePath = Path.GetRelativePath(Library.ProjectDirectoryPath, openFileDialog.FileName);
        }

        private bool NewKiCADSymbolLibraryCommandCanExecute(object? parameter)
        {
            if (this.NewKiCADSymbolLibraryName.Length > 0 && this.NewKiCADSymbolLibraryRelativePath.Length > 0)
                return !Library.KiCADSymbolLibraries.Any(kSL => kSL.Nickname.ToLower() == this.NewKiCADSymbolLibraryName.ToLower());
            else
                return false;
        }

        private void NewKiCADSymbolLibraryCommandExecuted(object? parameter)
        {
            int newIndex;
            for (newIndex = 0; newIndex < Library.KiCADSymbolLibraries.Count; newIndex++)
            {
                var compareKSL = Library.KiCADSymbolLibraries[newIndex];
                if (compareKSL.Nickname.CompareTo(this.NewKiCADSymbolLibraryName) > 0)
                    break;
            }

            KiCADSymbolLibrary newKiCADSymbolLibrary = new(this.NewKiCADSymbolLibraryName, this.NewKiCADSymbolLibraryRelativePath, Library);
            if (newIndex == Library.KiCADSymbolLibraries.Count)
                Library.KiCADSymbolLibraries.Add(newKiCADSymbolLibrary);
            else
                Library.KiCADSymbolLibraries.Insert(newIndex, newKiCADSymbolLibrary);

            this.NewKiCADSymbolLibraryName = "";
            this.NewKiCADSymbolLibraryRelativePath = "";
        }

        private bool UpdateKiCADSymbolLibraryCommandCanExecute(object? parameter)
        {
            if (SelectedKiCADSymbolLibrary is not null && this.NewKiCADSymbolLibraryName.Length > 0 && this.NewKiCADSymbolLibraryRelativePath.Length > 0)
            {
                var kiCADSymbolLibrariesWithSameName = Library.KiCADSymbolLibraries.Where(p => p.Nickname.ToLower() == this.NewKiCADSymbolLibraryName.ToLower()).ToArray();

                // Allow updates if none share the same name, or the name is the same as current, but the path is not (path case sensitive because of UNIX)
                return (kiCADSymbolLibrariesWithSameName.Length == 0) ||
                       (kiCADSymbolLibrariesWithSameName.Length == 1 && kiCADSymbolLibrariesWithSameName[0] == SelectedKiCADSymbolLibrary &&
                       this.NewKiCADSymbolLibraryRelativePath != SelectedKiCADSymbolLibrary.RelativePath);
            }
            else
                return false;
        }

        private void UpdateKiCADSymbolLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedKiCADSymbolLibrary is not null);
            SelectedKiCADSymbolLibrary.Nickname = this.NewKiCADSymbolLibraryName;
            SelectedKiCADSymbolLibrary.RelativePath = this.NewKiCADSymbolLibraryRelativePath;

            int oldIndex = Library.KiCADSymbolLibraries.IndexOf(SelectedKiCADSymbolLibrary);
            int newIndex = 0;
            for (int i = 0; i < Library.KiCADSymbolLibraries.Count; i++)
            {
                var compareKSL = Library.KiCADSymbolLibraries[i];
                if (compareKSL != SelectedKiCADSymbolLibrary)
                {
                    if (compareKSL.Nickname.CompareTo(this.NewKiCADSymbolLibraryName) > 0)
                        break;
                    newIndex++;
                }
            }
            if (oldIndex != newIndex)
                Library.KiCADSymbolLibraries.Move(oldIndex, newIndex);
        }

        private bool DeleteKiCADSymbolLibraryCommandCanExecute(object? parameter)
        {
            return SelectedKiCADSymbolLibrary is not null;
        }

        private void DeleteKiCADSymbolLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedKiCADSymbolLibrary is not null);
            Library.KiCADSymbolLibraries.Remove(SelectedKiCADSymbolLibrary);

            SelectedKiCADSymbolLibrary = Library.KiCADSymbolLibraries.FirstOrDefault();
        }

        private bool ReparseKiCADSymbolNamesCommandCanExecute(object? parameter)
        {
            return SelectedKiCADSymbolLibrary is not null;
        }

        private void ReparseKiCADSymbolNamesCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedKiCADSymbolLibrary is not null);
            SelectedKiCADSymbolLibrary.ParseKiCADSymbolNames();
        }

        private bool BrowseKiCADFootprintLibraryCommandCanExecute(object? parameter)
        {
            return Library.ProjectDirectoryPath != "";
        }

        private void BrowseKiCADFootprintLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(Library.ProjectDirectoryPath != "");

            // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
            OpenFolderDialog openFolderDialog = new();
            // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
            openFolderDialog.Title = "Open KiCAD Footprint Library Directory";
            if (openFolderDialog.ShowDialog() == true)
                this.NewKiCADFootprintLibraryRelativePath = Path.GetRelativePath(Library.ProjectDirectoryPath, openFolderDialog.FolderName);
        }

        private bool NewKiCADFootprintLibraryCommandCanExecute(object? parameter)
        {
            if (this.NewKiCADFootprintLibraryName.Length > 0 && this.NewKiCADFootprintLibraryRelativePath.Length > 0)
                return !Library.KiCADFootprintLibraries.Any(p => p.Nickname.ToLower() == this.NewKiCADFootprintLibraryName.ToLower());
            else
                return false;
        }

        private void NewKiCADFootprintLibraryCommandExecuted(object? parameter)
        {
            int newIndex;
            for (newIndex = 0; newIndex < Library.KiCADFootprintLibraries.Count; newIndex++)
            {
                var compareKFL = Library.KiCADFootprintLibraries[newIndex];
                if (compareKFL.Nickname.CompareTo(this.NewKiCADFootprintLibraryName) > 0)
                    break;
            }

            KiCADFootprintLibrary newKiCADFootprintLibrary = new(this.NewKiCADFootprintLibraryName, this.NewKiCADFootprintLibraryRelativePath, Library);
            if (newIndex == Library.KiCADFootprintLibraries.Count)
                Library.KiCADFootprintLibraries.Add(newKiCADFootprintLibrary);
            else
                Library.KiCADFootprintLibraries.Insert(newIndex, newKiCADFootprintLibrary);

            this.NewKiCADFootprintLibraryName = "";
            this.NewKiCADFootprintLibraryRelativePath = "";
        }

        private bool UpdateKiCADFootprintLibraryCommandCanExecute(object? parameter)
        {
            if (SelectedKiCADFootprintLibrary is not null && this.NewKiCADFootprintLibraryName.Length > 0 && this.NewKiCADFootprintLibraryRelativePath.Length > 0)
            {
                var kiCADFootprintLibrariesWithSameName = Library.KiCADFootprintLibraries.Where(p => p.Nickname.ToLower() == this.NewKiCADFootprintLibraryName.ToLower()).ToArray();

                // Allow updates if none share the same name, or the name is the same as current, but the path is not (path case sensitive because of UNIX)
                return (kiCADFootprintLibrariesWithSameName.Length == 0) ||
                       (kiCADFootprintLibrariesWithSameName.Length == 1 && kiCADFootprintLibrariesWithSameName[0] == SelectedKiCADFootprintLibrary &&
                       this.NewKiCADFootprintLibraryRelativePath != SelectedKiCADFootprintLibrary.RelativePath);
            }
            else
                return false;
        }

        private void UpdateKiCADFootprintLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedKiCADFootprintLibrary is not null);
            SelectedKiCADFootprintLibrary.Nickname = this.NewKiCADFootprintLibraryName;
            SelectedKiCADFootprintLibrary.RelativePath = this.NewKiCADFootprintLibraryRelativePath;

            int oldIndex = Library.KiCADFootprintLibraries.IndexOf(SelectedKiCADFootprintLibrary);
            int newIndex = 0;
            for (int i = 0; i < Library.KiCADFootprintLibraries.Count; i++)
            {
                var compareKFL = Library.KiCADFootprintLibraries[i];
                if (compareKFL != SelectedKiCADFootprintLibrary)
                {
                    if (compareKFL.Nickname.CompareTo(this.NewKiCADFootprintLibraryName) > 0)
                        break;
                    newIndex++;
                }
            }
            if (oldIndex != newIndex)
                Library.KiCADFootprintLibraries.Move(oldIndex, newIndex);
        }

        private bool DeleteKiCADFootprintLibraryCommandCanExecute(object? parameter)
        {
            return SelectedKiCADFootprintLibrary is not null;
        }

        private void DeleteKiCADFootprintLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedKiCADFootprintLibrary is not null);
            Library.KiCADFootprintLibraries.Remove(SelectedKiCADFootprintLibrary);

            SelectedKiCADFootprintLibrary = Library.KiCADFootprintLibraries.FirstOrDefault();
        }

        private bool ReparseKiCADFootprintNamesCommandCanExecute(object? parameter)
        {
            return SelectedKiCADFootprintLibrary is not null;
        }

        private void ReparseKiCADFootprintNamesCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedKiCADFootprintLibrary is not null);
            SelectedKiCADFootprintLibrary.ParseKiCADFootprintNames();
        }

        #endregion Commands
    }
}
