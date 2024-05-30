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

namespace KiCAD_DB_Editor.ViewModel
{
    public class SubLibraryVM : NotifyObject, IComparable<SubLibraryVM>
    {
        public readonly Model.SubLibrary SubLibrary;
        private ViewModel.SubLibraryVM? _parentSubLibraryVM;
        public ViewModel.SubLibraryVM? ParentSubLibraryVM
        {
            get { return _parentSubLibraryVM; }
            set
            {
                if (_parentSubLibraryVM != value)
                {
                    _parentSubLibraryVM = value;

                    InvokePropertyChanged(nameof(SubLibraryVM.Path));
                    RefreshInheritedParameters();
                }
            }
        }

        #region Notify Properties

        private IEnumerable<ParameterVM>? _cachedInheritedParameterVMs;
        public IEnumerable<ParameterVM> InheritedParameterVMs
        {
            get
            {
                if (_cachedInheritedParameterVMs is null)
                {
                    if (ParentSubLibraryVM is null)
                        _cachedInheritedParameterVMs = Enumerable.Empty<ParameterVM>();
                    else
                        _cachedInheritedParameterVMs = ParentSubLibraryVM.InheritedParameterVMs.Concat(ParentSubLibraryVM.ParameterVMs).OrderBy(pVM => pVM);
                }
                return _cachedInheritedParameterVMs;
            }
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

                    _parameterVMs_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    InvokePropertyChanged();
                }
            }
        }

        // Do not initialise here, do in constructor to link collection changed
        private ObservableCollectionEx<SubLibraryVM> _subLibraryVMs;
        public ObservableCollectionEx<SubLibraryVM> SubLibraryVMs
        {
            get { return _subLibraryVMs; }
            set
            {
                if (_subLibraryVMs != value)
                {
                    if (_subLibraryVMs is not null)
                        _subLibraryVMs.CollectionChanged -= _subLibraryVMs_CollectionChanged;
                    _subLibraryVMs = value;
                    _subLibraryVMs.CollectionChanged += _subLibraryVMs_CollectionChanged;

                    _subLibraryVMs_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    InvokePropertyChanged();
                }
            }
        }

        public string Name
        {
            get { return SubLibrary.Name; }
            set
            {
                if (SubLibrary.Name != value)
                {
                    try
                    {
                        if (EditCommand.CanExecute(value))
                            EditCommand.Execute(value);
                    }
                    catch (ArgumentValidationException ex)
                    {
                        // Breaks MVVM but not worth the effort to respect MVVM for this
                        (new Window_ErrorDialog(ex.Message)).ShowDialog();
                    }
                }
            }
        }

        public string Path
        {
            get { return ParentSubLibraryVM is null ? $"{Name}/" : $"{ParentSubLibraryVM.Path}{Name}/"; }
        }

        #endregion Notify Properties

        private void _parameterVMs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            SubLibrary.Parameters = ParameterVMs.Select(pVM => pVM.Parameter).ToList();
            RefreshInheritedParameters();
        }

        private List<SubLibraryVM> subLibraryVMsSubscribed = new();
        private void _subLibraryVMs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            subLibraryVMsSubscribed.ForEach(slVM => slVM.PropertyChanged -= subLibraryVM_PropertyChanged);
            subLibraryVMsSubscribed.Clear();

            SubLibrary.SubLibraries = SubLibraryVMs.Select(slVM => slVM.SubLibrary).ToList();

            foreach (var subLibraryVM in SubLibraryVMs)
            {
                subLibraryVMsSubscribed.Add(subLibraryVM);
                subLibraryVM.PropertyChanged += subLibraryVM_PropertyChanged;
            }
        }

        private void subLibraryVM_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SubLibrary.Name):
                    SubLibraryVMs = new ObservableCollectionEx<SubLibraryVM>(SubLibraryVMs.OrderBy(slVM => slVM));
                    break;
            }
        }

        public SubLibraryVM(ViewModel.SubLibraryVM? parentSubLibraryVM, Model.SubLibrary subLibrary)
        {
            // Link model
            SubLibrary = subLibrary;

            ParentSubLibraryVM = parentSubLibraryVM;

            // Initialise collection with events
            // Technically overwrites SubLibrary.SubLibraries even though it was just assigned above
            SubLibraryVMs = new(subLibrary.SubLibraries.OrderBy(sL => sL.Name).Select(sL => new SubLibraryVM(this, sL)));
            Debug.Assert(_subLibraryVMs is not null);
            // Technically overwrites SubLibrary.Parameters even though it was just assigned above
            ParameterVMs = new(subLibrary.Parameters.OrderBy(p => p.Name).Select(p => new ParameterVM(this, p)));
            Debug.Assert(_parameterVMs is not null);

            // Setup commands
            RemoveSubLibraryCommand = new BasicCommand(RemoveSubLibraryCommandExecuted, RemoveSubLibraryCommandCanExecute);
            AddSubLibraryCommand = new BasicCommand(AddSubLibraryCommandExecuted, AddSubLibraryCommandCanExecute);
            EditCommand = new BasicCommand(EditCommandExecuted, EditCommandCanExecute);
            AddParameterCommand = new BasicCommand(AddParameterCommandExecuted, AddParameterCommandCanExecute);
        }

        public bool RecursiveContains(SubLibraryVM otherSLVM)
        {
            if (this == otherSLVM)
                return true;
            else
                return SubLibraryVMs.Aggregate(false, (contains, slVM) => contains || slVM.RecursiveContains(otherSLVM));
        }

        public int CompareTo(SubLibraryVM? other)
        {
            if (other is null)
                return 1;
            else
                return this.Name.CompareTo(other.Name);
        }

        public void RefreshInheritedParameters()
        {
            _cachedInheritedParameterVMs = null;
            this.InvokePropertyChanged(nameof(SubLibraryVM.InheritedParameterVMs));
            if (SubLibraryVMs is not null)
                foreach (var subLibraryVM in SubLibraryVMs)
                    subLibraryVM.RefreshInheritedParameters();
        }

        // Method instead of property as we don't want to allow binding as when we change a SLVM's parent the recursive becomes out of date on old and new parent and I don't want to deal with it
        public IEnumerable<ParameterVM> RecursiveParameterVMs()
        {
            return ParameterVMs.Concat(SubLibraryVMs.Aggregate(Enumerable.Empty<ParameterVM>(), (acc, slVM) => acc.Concat(slVM.RecursiveParameterVMs()))).OrderBy(pVM => pVM);
        }


        #region Commands

        public IBasicCommand RemoveSubLibraryCommand { get; }
        public IBasicCommand AddSubLibraryCommand { get; }
        public IBasicCommand EditCommand { get; }
        public IBasicCommand AddParameterCommand { get; }

        private bool RemoveSubLibraryCommandCanExecute(object? parameter)
        {
            return parameter is SubLibraryVM slVM && SubLibraryVMs.Contains(slVM);
        }

        private void RemoveSubLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(parameter is SubLibraryVM);
            var slVM = (SubLibraryVM)parameter;
            Debug.Assert(SubLibraryVMs.Contains(slVM));

            this.SubLibraryVMs.Remove(slVM);
        }

        private bool AddSubLibraryCommandCanExecute(object? parameter)
        {
            return parameter is SubLibraryVM slVM && !SubLibraryVMs.Where(oSlVM => oSlVM.Name.Equals(slVM.Name, StringComparison.OrdinalIgnoreCase)).Any() &&
                !InheritedParameterVMs.Concat(ParameterVMs).Select(pVM => pVM.Name).Intersect(slVM.RecursiveParameterVMs().Select(pVM => pVM.Name), StringComparer.OrdinalIgnoreCase).Any();
        }

        private void AddSubLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(parameter is SubLibraryVM);
            var slVM = (SubLibraryVM)parameter;
            Debug.Assert(!SubLibraryVMs.Where(oSlVM => oSlVM.Name.Equals(slVM.Name, StringComparison.OrdinalIgnoreCase)).Any());
            Debug.Assert(!InheritedParameterVMs.Concat(ParameterVMs).Select(pVM => pVM.Name).Intersect(slVM.RecursiveParameterVMs().Select(pVM => pVM.Name), StringComparer.OrdinalIgnoreCase).Any());

            int index = ~this.SubLibraryVMs.BinarySearchIndexOf(slVM);
            this.SubLibraryVMs.Insert(index, slVM);
            slVM.ParentSubLibraryVM = this;
        }

        private bool EditCommandCanExecute(object? parameter)
        {
            return parameter is string newName && ParentSubLibraryVM is not null;
        }

        private void EditCommandExecuted(object? parameter)
        {
            Debug.Assert(parameter is string);
            string newName = (string)parameter;
            Debug.Assert(ParentSubLibraryVM is not null);

            if (newName.Length < 1 || newName.Length > 100)
                throw new ArgumentValidationException("New name is invalid length");
            if (ParentSubLibraryVM.SubLibraryVMs.Where(oSlVM => oSlVM.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)).Any())
                throw new ArgumentValidationException("New name conflicts with existing sub-folder");

            SubLibrary.Name = newName;
            InvokePropertyChanged(nameof(SubLibraryVM.Name));
            InvokePropertyChanged(nameof(SubLibraryVM.Path));
        }

        private bool AddParameterCommandCanExecute(object? parameter)
        {
            return true;
        }

        private void AddParameterCommandExecuted(object? parameter)
        {
            ParameterVM pVM = new(this, new(""));

            int n = 1;
            while (true)
            {
                string trialName = $"param{n}";
                try
                {
                    if (pVM.EditCommand.CanExecute(trialName))
                    {
                        pVM.EditCommand.Execute(trialName);
                        break;
                    }
                }
                catch (ArgumentValidationException)
                {
                    n++;
                }
            }
            ParameterVMs.Add(pVM);
        }

        #endregion Commands


    }
}
