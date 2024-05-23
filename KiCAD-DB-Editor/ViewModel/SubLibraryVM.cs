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

namespace KiCAD_DB_Editor.ViewModel
{
    public class SubLibraryVM : NotifyObject, IComparable<SubLibraryVM>
    {
        public readonly Model.SubLibrary SubLibrary;
        public ViewModel.SubLibraryVM? ParentSubLibraryVM;

        #region Notify Properties

        public IEnumerable<ParameterVM> InheritedParameterVMs
        {
            get
            {
                if (ParentSubLibraryVM is null)
                    return Array.Empty<ParameterVM>();
                else
                {
                    return ParentSubLibraryVM.InheritedParameterVMs.Concat(ParentSubLibraryVM.ParameterVMs).OrderBy(pVM => pVM);
                }
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
                    foreach (var subLibraryVM in SubLibraryVMs)
                        subLibraryVM.InvokePropertyChanged(nameof(SubLibraryVM.InheritedParameterVMs));
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
            set { if (SubLibrary.Name != value) { SubLibrary.Name = value; InvokePropertyChanged(); } }
        }

        #endregion Notify Properties

        private void _parameterVMs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            SubLibrary.Parameters = ParameterVMs.Select(pVM => pVM.Parameter).ToList();
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
            ParameterVMs = new(subLibrary.Parameters.OrderBy(p => p.Name).Select(p => new ParameterVM(p)));
            Debug.Assert(_parameterVMs is not null);

            // Setup commands
            RemoveSubLibraryCommand = new BasicCommand(RemoveSubLibraryCommandExecuted, RemoveSubLibraryCommandCanExecute);
            AddSubLibraryCommand = new BasicCommand(AddSubLibraryCommandExecuted, AddSubLibraryCommandCanExecute);
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


        #region Commands

        public IBasicCommand RemoveSubLibraryCommand { get; }
        public IBasicCommand AddSubLibraryCommand { get; }

        private bool RemoveSubLibraryCommandCanExecute(object? parameter)
        {
            return parameter is SubLibraryVM;
        }

        private void RemoveSubLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(parameter is SubLibraryVM);

            var slVM = (SubLibraryVM)parameter;
            this.SubLibraryVMs.Remove(slVM);
        }

        private bool AddSubLibraryCommandCanExecute(object? parameter)
        {
            return parameter is SubLibraryVM;
        }

        private void AddSubLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(parameter is SubLibraryVM);

            var slVM = (SubLibraryVM)parameter;
            int index = ~this.SubLibraryVMs.BinarySearchIndexOf(slVM);
            this.SubLibraryVMs.Insert(index, slVM);
            slVM.ParentSubLibraryVM = this;
        }

        #endregion Commands
    }
}
