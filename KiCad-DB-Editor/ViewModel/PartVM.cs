using KiCad_DB_Editor.Model;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace KiCad_DB_Editor.ViewModel
{
    public class PartVM : NotifyObject
    {
        public ParameterAccessor ParameterAccessor { get; }
        public FootprintLibraryNameAccessor FootprintLibraryNameAccessor { get; }
        public FootprintNameAccessor FootprintNameAccessor { get; }
        public SelectedFootprintLibraryAccessor SelectedFootprintLibraryAccessor { get; }
        public Part Part { get; }

        #region Notify Properties

        public string Path
        {
            get
            {
                string path = this.Part.ParentCategory.Name;
                var c = Part.ParentCategory.ParentCategory;
                while (c is not null)
                {
                    path = $"{c.Name}/{path}";
                    c = c.ParentCategory;
                }
                return path;
            }
        }

        public string SymbolLibraryName
        {
            get { return Part.SymbolLibraryName; }
            set
            {
                if (Part.SymbolLibraryName != value)
                {
                    Part.SymbolLibraryName = value;
                    // We don't InvokePropertyChanged here, as making the change to the Part will trigger Part_PropertyChanged
                }
            }
        }

        // Included so the KiCad symbol name drop down has a source
        public KiCadSymbolLibrary? SelectedKiCadSymbolLibrary
        {
            // Have to do ! as FirstOrDefault needs to think kSLVM could be null in order for me to return null
            get { return Part.ParentLibrary.KiCadSymbolLibraries.FirstOrDefault(kSLVM => kSLVM!.Nickname == SymbolLibraryName, null); }
        }

        public string SymbolName
        {
            get { return Part.SymbolName; }
            set { if (Part.SymbolName != value) { Part.SymbolName = value; InvokePropertyChanged(); } }
        }

        public int FootprintCount
        {
            get { return Part.FootprintPairs.Count; }
        }

        #endregion Notify Properties

        public PartVM(Model.Part part)
        {
            // Link model
            Part = part;

            Part.PropertyChanged += Part_PropertyChanged;
            // We rely on FootprintLibraryNames and FootprintNames changing at the same time, so we don't subscribe to both
            Part.FootprintPairs.CollectionChanged += FootprintPairs_CollectionChanged;

            ParameterAccessor = new(this);
            FootprintLibraryNameAccessor = new(this);
            FootprintNameAccessor = new(this);
            SelectedFootprintLibraryAccessor = new(this);
        }

        public void Unsubscribe()
        {
            Part.PropertyChanged -= Part_PropertyChanged;
            Part.FootprintPairs.CollectionChanged -= FootprintPairs_CollectionChanged;
        }

        private void FootprintPairs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            InvokePropertyChanged(nameof(this.FootprintCount));

            // This is needed to update the table's existing cells
            FootprintLibraryNameAccessor.InvokePropertyChanged("Item[]");
            FootprintNameAccessor.InvokePropertyChanged("Item[]");
        }

        private void Part_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Part.SymbolLibraryName):
                    // We have to wrap this, as at the VM level we add SelectedKiCadSymbolLibrary
                    InvokePropertyChanged(nameof(PartVM.SymbolLibraryName));

                    // Doesn't seem to be technically required as the bindings for the ComboBoxes I'm designing this for only load
                    // when the cells are edited, but if not then I'd need to do this to prompt the ComboBoxes to refetch the value
                    // On future investigation, it's clear that I can't switch to a system where the ComboBoxes are persistent. WPF is
                    // weird: when I clear the SelectedKiCadSymbolLibraryVM, the available items in the symbol name should be blank
                    // and it does do this, but if the current text is one of those items, it will get cleared, which is not what
                    // I want at all
                    InvokePropertyChanged(nameof(this.SelectedKiCadSymbolLibrary));

                    // Changing the selected symbol library should prompt clearing the symbol name
                    SymbolName = "";
                    break;
            }
        }

        public void InvokePropertyChanged_Path()
        {
            InvokePropertyChanged(nameof(this.Path));
        }

        #region Commands



        #endregion Commands

    }

    public class ParameterAccessor : NotifyObject
    {
        public readonly PartVM OwnerPartVM;

        #region Notify Properties

        public string? this[string parameterUUID]
        {
            get
            {
                Parameter? parameter = OwnerPartVM.Part.ParentLibrary.AllParameters.FirstOrDefault(p => p!.UUID == parameterUUID, null);
                Debug.Assert(parameter is not null); // I want to know if this is null
                if (OwnerPartVM.Part.ParameterValues.TryGetValue(parameter, out string? val))
                    return val;
                else
                    return null;
            }
            set
            {
                if (value is not null)
                {
                    Parameter parameter = OwnerPartVM.Part.ParentLibrary.AllParameters.First(p => p.UUID == parameterUUID);
                    if (OwnerPartVM.Part.ParameterValues.TryGetValue(parameter, out string? s))
                    {
                        if (s != value)
                        {
                            OwnerPartVM.Part.ParameterValues[parameter] = value;
                            InvokePropertyChanged("Item[]");
                        }
                    }
                }
            }
        }

        #endregion Notify Properties

        public ParameterAccessor(PartVM ownerPVM)
        {
            OwnerPartVM = ownerPVM;
        }
    }

    public class FootprintLibraryNameAccessor : NotifyObject
    {
        public readonly PartVM OwnerPartVM;

        #region Notify Properties

        public string? this[int index]
        {
            get
            {
                if (OwnerPartVM.Part.FootprintPairs.Count > index)
                    return OwnerPartVM.Part.FootprintPairs[index].Item1;
                else
                    return null;
            }
            set
            {
                if (value is not null)
                {
                    if (OwnerPartVM.Part.FootprintPairs.Count > index)
                    {
                        // Changing the selected footprint library should prompt clearing the footprint name
                        OwnerPartVM.Part.FootprintPairs[index] = (value, "");
                        InvokePropertyChanged("Item[]");
                        OwnerPartVM.FootprintNameAccessor.InvokePropertyChanged("Item[]");

                        // Doesn't seem to be technically required as the bindings for the ComboBoxes I'm designing this for only load
                        // when the cells are edited, but if not then I'd need to do this to prompt the ComboBoxes to refetch the value
                        // On future investigation, it's clear that I can't switch to a system where the ComboBoxes are persistent. WPF is
                        // weird: when I clear the SelectedKiCadFootprintLibrary, the available items in the footprint name should be blank
                        // and it does do this, but if the current text is one of those items, it will get cleared, which is not what
                        // I want at all
                        OwnerPartVM.SelectedFootprintLibraryAccessor.InvokePropertyChanged("Item[]");
                    }
                }
            }
        }

        #endregion Notify Properties

        public FootprintLibraryNameAccessor(PartVM ownerPVM)
        {
            OwnerPartVM = ownerPVM;
        }
    }

    public class FootprintNameAccessor : NotifyObject
    {
        public readonly PartVM OwnerPartVM;

        #region Notify Properties

        public string? this[int index]
        {
            get
            {
                if (OwnerPartVM.Part.FootprintPairs.Count > index)
                    return OwnerPartVM.Part.FootprintPairs[index].Item2;
                else
                    return null;
            }
            set
            {
                if (value is not null)
                {
                    if (OwnerPartVM.Part.FootprintPairs.Count > index)
                    {
                        OwnerPartVM.Part.FootprintPairs[index] = (OwnerPartVM.Part.FootprintPairs[index].Item1, value);
                        InvokePropertyChanged("Item[]");
                    }
                }
            }
        }

        #endregion Notify Properties

        public FootprintNameAccessor(PartVM ownerPVM)
        {
            OwnerPartVM = ownerPVM;
        }
    }

    public class SelectedFootprintLibraryAccessor : NotifyObject
    {
        public readonly PartVM OwnerPartVM;

        #region Notify Properties

        public KiCadFootprintLibrary? this[int index]
        {
            get
            {
                if (OwnerPartVM.Part.FootprintPairs.Count > index)
                    // Have to do ! as FirstOrDefault needs to think kSLVM could be null in order for me to return null
                    return OwnerPartVM.Part.ParentLibrary.KiCadFootprintLibraries.FirstOrDefault(kFL => kFL!.Nickname == OwnerPartVM.Part.FootprintPairs[index].Item1, null);
                else
                    return null;
            }
        }

        #endregion Notify Properties

        public SelectedFootprintLibraryAccessor(PartVM ownerPVM)
        {
            OwnerPartVM = ownerPVM;
        }
    }
}
