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
using KiCad_DB_Editor.Commands;
using KiCad_DB_Editor.View;
using KiCad_DB_Editor.Utilities;
using KiCad_DB_Editor.Exceptions;
using KiCad_DB_Editor.Model;
using KiCad_DB_Editor.View.Dialogs;
using System.Security.Cryptography;
using System.Windows.Media;
using System.Formats.Asn1;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Threading;

namespace KiCad_DB_Editor.ViewModel
{
    public class CategoryVM : NotifyObject
    {
        public Category Category { get; }
        private DispatcherTimer _newPartsLoopTimer;
        private int _newPartsRemainingNumNewParts;

        #region Notify Properties

        private string? _pathCache = null;
        public string Path
        {
            get
            {
                if (_pathCache is not null)
                    return _pathCache;

                string path = this.Category.Name;
                var c = this.Category.ParentCategory;
                while (c is not null)
                {
                    path = $"{c.Name}/{path}";
                    c = c.ParentCategory;
                }
                _pathCache = path;
                return path;
            }
        }

        private int _numNewParts = 1;
        public int NumNewParts
        {
            get { return _numNewParts; }
            set
            {
                if (_numNewParts != value)
                {
                    if (value < 1)
                        throw new Exceptions.ArgumentValidationException("Number of new parts can't be less than 1");
                    else if (value > 10_000)
                        throw new Exceptions.ArgumentValidationException("Number of new parts can't be more than 10,000");

                    _numNewParts = value;
                    InvokePropertyChanged();
                }
            }
        }

        private ObservableCollectionEx<CategoryVM> _categoryVMs;
        public ObservableCollectionEx<CategoryVM> CategoryVMs
        {
            get { return _categoryVMs; }
            set
            {
                if (_categoryVMs != value)
                {
                    // Make sure CategoryVM unsubscribes before we lose the objects
                    if (_categoryVMs is not null) foreach (var cVM in _categoryVMs) cVM.Unsubscribe();

                    _categoryVMs = value;
                    InvokePropertyChanged();
                }
            }
        }

        private ObservableCollectionEx<PartVM> _partVMs;
        public ObservableCollectionEx<PartVM> PartVMs
        {
            get { return _partVMs; }
            set
            {
                if (_partVMs != value)
                {
                    // Make sure PartVM unsubscribes before we lose the objects
                    if (_partVMs is not null) foreach (var pVM in _partVMs) pVM.Unsubscribe();

                    _partVMs = value;
                    InvokePropertyChanged();
                }
            }
        }

        private string? _selectedParameter = null;
        public string? SelectedParameter
        {
            get { return _selectedParameter; }
            set { if (_selectedParameter != value) { _selectedParameter = value; InvokePropertyChanged(); } }
        }

        private PartVM[] _selectedPartVMs = Array.Empty<PartVM>();
        public PartVM[] SelectedPartVMs
        {
            // For some reason I can't do OneWayToSource :(
            get { return _selectedPartVMs; }
            set { if (_selectedPartVMs != value) { _selectedPartVMs = value; InvokePropertyChanged(); } }
        }

        #endregion Notify Properties

        public CategoryVM(Model.Category category)
        {
            // Link model
            Category = category;

            Category.PropertyChanged += Category_PropertyChanged;

            Category.Categories.CollectionChanged += Categories_CollectionChanged;
            CategoryVMs = new(Category.Categories.Select(c => new CategoryVM(c)));
            Debug.Assert(_categoryVMs is not null);

            Category.Parts.CollectionChanged += Parts_CollectionChanged;
            PartVMs = new(Category.Parts.Select(p => new PartVM(p)));
            Debug.Assert(_partVMs is not null);

            // Setup commands
            NewPartsCommand = new BasicCommand(NewPartsCommandExecuted, null);
            DuplicatePartCommand = new BasicCommand(DuplicatePartCommandExecuted, DuplicatePartCommandCanExecute);
            DeletePartsCommand = new BasicCommand(DeletePartsCommandExecuted, DeletePartsCommandCanExecute);
            AddFootprintCommand = new BasicCommand(AddFootprintCommandExecuted, AddFootprintCommandCanExecute);
            RemoveFootprintCommand = new BasicCommand(RemoveFootprintCommandExecuted, RemoveFootprintCommandCanExecute);

            _newPartsLoopTimer = new();
            // Guarantees generated part numbers are at least 1 millisecond apart
            // When testing the loop fired much less frequently than that anyway, so probably not that critical
            _newPartsLoopTimer.Interval = TimeSpan.FromMilliseconds(2);
            _newPartsLoopTimer.Tick += _newPartsLoopTimer_Tick;
        }

        public void Unsubscribe()
        {
            Category.PropertyChanged -= Category_PropertyChanged;
            Category.Categories.CollectionChanged -= Parts_CollectionChanged;
            Category.Parts.CollectionChanged -= Parts_CollectionChanged;
            foreach (var cVM in CategoryVMs) cVM.Unsubscribe();
            foreach (var pVM in PartVMs) pVM.Unsubscribe();
        }

        private void Parts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                // Handle some of these more precisely than Reset for efficiency
                case NotifyCollectionChangedAction.Add:
                    Debug.Assert(e.NewItems is not null && e.NewItems.Count == 1);
                    Part newPart = (e.NewItems[0] as Part)!;
                    PartVMs.Insert(e.NewStartingIndex, new(newPart));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    // Rely on the indexes being the same as the source Parts list
                    var pVMToRemove = PartVMs[e.OldStartingIndex];
                    pVMToRemove.Unsubscribe();
                    PartVMs.RemoveAt(e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Reset:
                default:
                    PartVMs = new(Category.Parts.Select(p => new PartVM(p)));
                    break;
            }
        }

        private void Categories_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // If we reset the list the TreeView selection is lost, so we need to handle proper collection changed events
            // At least .Move, but we may as well do more
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Move:
                    // Rely on the indexes being the same as the source Categories list
                    CategoryVMs.Move(e.OldStartingIndex, e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    // Rely on the indexes being the same as the source Categories list
                    var cVMToRemove = CategoryVMs[e.OldStartingIndex];
                    cVMToRemove.Unsubscribe();
                    CategoryVMs.RemoveAt(e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Add:
                    // Rely on the indexes being the same as the source Categories list
                    Debug.Assert(e.NewItems is not null && e.NewItems.Count == 1);
                    var newCategory = (e.NewItems[0] as Category)!;
                    var cVMToAdd = new CategoryVM(newCategory);
                    CategoryVMs.Insert(e.NewStartingIndex, cVMToAdd);
                    break;
                case NotifyCollectionChangedAction.Replace:
                default:
                    CategoryVMs = new(Category.Categories.Select(c => new CategoryVM(c)));
                    break;
            }
        }

        private void Category_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Category.Name):
                    InvokePropertyChanged_Path();
                    break;
                    // Categories, Parameters, Parts do not have setter so we don't need to listen here
            }
        }

        public void InvokePropertyChanged_Path()
        {
            // Need to remember to void the Path cache
            _pathCache = null;
            InvokePropertyChanged(nameof(this.Path));
            foreach (CategoryVM cVM in CategoryVMs)
                cVM.InvokePropertyChanged_Path();
            foreach (PartVM pVM in PartVMs)
                pVM.InvokePropertyChanged_Path();
        }

        private void _newPartsLoopTimer_Tick(object? sender, EventArgs e)
        {
            if (_newPartsRemainingNumNewParts > 0)
            {
                _newPart();
                _newPartsRemainingNumNewParts--;
            }

            if (_newPartsRemainingNumNewParts == 0)
                _newPartsLoopTimer.Stop();
        }

        private void _newPart(Part? existingPartToDuplicate = null)
        {
            string partUID = Util.GeneratePartUID(Category.ParentLibrary.PartUIDScheme);
            Part part = new(partUID, Category.ParentLibrary, Category);
            foreach (string parameter in Category.InheritedAndNormalParameters)
                part.ParameterValues.Add(parameter, "");
            if (existingPartToDuplicate is not null)
                part.CopyFromPart(existingPartToDuplicate);
            Category.Parts.Add(part);
            Category.ParentLibrary.AllParts.Add(part);
        }

        #region Commands

        public IBasicCommand NewPartsCommand { get; }
        public IBasicCommand DuplicatePartCommand { get; }
        public IBasicCommand DeletePartsCommand { get; }
        public IBasicCommand AddFootprintCommand { get; }
        public IBasicCommand RemoveFootprintCommand { get; }

        private void NewPartsCommandExecuted(object? _)
        {
            if (NumNewParts == 1)
                _newPart();
            else
            {
                _newPartsRemainingNumNewParts += NumNewParts;
                _newPartsLoopTimer.Start();
            }
        }

        private bool DuplicatePartCommandCanExecute(object? parameter)
        {
            return SelectedPartVMs.Length == 1;
        }

        private void DuplicatePartCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedPartVMs.Length == 1);
            Part partToBeDuplicated = SelectedPartVMs[0].Part;
            _newPart(partToBeDuplicated);
        }

        private bool DeletePartsCommandCanExecute(object? parameter)
        {
            return SelectedPartVMs.Length > 0;
        }

        private void DeletePartsCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedPartVMs.Length > 0);
            List<Part> psToBeRemoved = new(SelectedPartVMs.Select(pVM => pVM.Part));
            foreach (Part pToBeRemoved in psToBeRemoved)
            {
                Category.Parts.Remove(pToBeRemoved);
                Category.ParentLibrary.AllParts.Remove(pToBeRemoved);
            }
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

        #endregion Commands
    }
}
