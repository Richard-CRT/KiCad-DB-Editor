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
            set { if (_categoryVMs != value) { _categoryVMs = value; InvokePropertyChanged(); } }
        }

        private ObservableCollectionEx<PartVM> _partVMs;
        public ObservableCollectionEx<PartVM> PartVMs
        {
            get { return _partVMs; }
            set { if (_partVMs != value) { _partVMs = value; InvokePropertyChanged(); } }
        }

        private Parameter? _selectedUnusedParameter = null;
        public Parameter? SelectedUnusedParameter
        {
            get { return _selectedUnusedParameter; }
            set { if (_selectedUnusedParameter != value) { _selectedUnusedParameter = value; InvokePropertyChanged(); } }
        }

        private Parameter? _selectedParameter = null;
        public Parameter? SelectedParameter
        {
            get { return _selectedParameter; }
            set { if (_selectedParameter != value) { _selectedParameter = value; InvokePropertyChanged(); } }
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

        public CategoryVM(Model.Category category)
        {
            // Link model
            Category = category;

            Category.PropertyChanged += Category_PropertyChanged;

            Category.Categories.CollectionChanged += Categories_CollectionChanged;
            // Make sure CategoryVM unsubscribes before we lose the objects
            if (CategoryVMs is not null) foreach (var cVM in CategoryVMs) cVM.Unsubscribe();
            CategoryVMs = new(Category.Categories.Select(c => new CategoryVM(c)));
            Debug.Assert(_categoryVMs is not null);

            Category.Parts.CollectionChanged += Parts_CollectionChanged;
            // Make sure PartVM unsubscribes before we lose the objects
            if (PartVMs is not null) foreach (var pVM in PartVMs) pVM.Unsubscribe();
            PartVMs = new(Category.Parts.Select(p => new PartVM(p)));
            Debug.Assert(_partVMs is not null);

            // Setup commands
            AddParameterCommand = new BasicCommand(AddParameterCommandExecuted, AddParameterCommandCanExecute);
            RemoveParameterCommand = new BasicCommand(RemoveParameterCommandExecuted, RemoveParameterCommandCanExecute);
            NewPartsCommand = new BasicCommand(NewPartsCommandExecuted, null);
            DeletePartsCommand = new BasicCommand(DeletePartCommandExecuted, DeletePartsCommandCanExecute);
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
                    PartVMs.RemoveAt(e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Reset:
                default:
                    // Make sure PartVM unsubscribes before we lose the objects
                    if (PartVMs is not null) foreach (var pVM in PartVMs) pVM.Unsubscribe();
                    PartVMs = new(Category.Parts.Select(p => new PartVM(p)));
                    break;
            }
        }

        private void Categories_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Make sure CategoryVM unsubscribes before we lose the objects
            if (CategoryVMs is not null) foreach (var cVM in CategoryVMs) cVM.Unsubscribe();
            CategoryVMs = new(Category.Categories.Select(c => new CategoryVM(c)));
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

        private void _newPart()
        {
            string partUID = Util.GeneratePartUID(Category.ParentLibrary.PartUIDScheme);
            Part part = new(partUID, Category.ParentLibrary, Category);
            foreach (Parameter parameter in Category.InheritedAndNormalParameters)
                part.ParameterValues.Add(parameter, "");
            Category.Parts.Add(part);
            Category.ParentLibrary.AllParts.Add(part);
        }

        #region Commands

        public IBasicCommand AddParameterCommand { get; }
        public IBasicCommand RemoveParameterCommand { get; }
        public IBasicCommand NewPartsCommand { get; }
        public IBasicCommand DeletePartsCommand { get; }
        public IBasicCommand AddFootprintCommand { get; }
        public IBasicCommand RemoveFootprintCommand { get; }

        private bool AddParameterCommandCanExecute(object? parameter)
        {
            return SelectedUnusedParameter is not null;
        }

        private void AddParameterCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedUnusedParameter is not null);

            Parameter pToBeAdded = SelectedUnusedParameter;

            int indexOfPToBeAddedInLibrary = Category.ParentLibrary.AllParameters.IndexOf(pToBeAdded);
            int newIndex;
            for (newIndex = 0; newIndex < Category.Parameters.Count; newIndex++)
            {
                if (indexOfPToBeAddedInLibrary < Category.ParentLibrary.AllParameters.IndexOf(Category.Parameters[newIndex]))
                {
                    break;
                }
            }
            if (newIndex == Category.Parameters.Count)
                Category.Parameters.Add(pToBeAdded);
            else
                Category.Parameters.Insert(newIndex, pToBeAdded);

            SelectedUnusedParameter = Category.AvailableParameters.FirstOrDefault();
        }

        private bool RemoveParameterCommandCanExecute(object? parameter)
        {
            return SelectedParameter is not null;
        }

        private void RemoveParameterCommandExecuted(object? parameter)
        {
            Debug.Assert(SelectedParameter is not null);

            Parameter pToBeRemoved = SelectedParameter;

            Category.Parameters.Remove(pToBeRemoved);

            SelectedParameter = Category.Parameters.FirstOrDefault();
        }

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

        private bool DeletePartsCommandCanExecute(object? parameter)
        {
            return SelectedPartVMs.Length > 0;
        }

        private void DeletePartCommandExecuted(object? parameter)
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
