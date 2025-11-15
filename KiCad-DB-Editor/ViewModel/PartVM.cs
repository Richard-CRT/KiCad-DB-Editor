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

        private string? _pathCache = null;
        public string Path
        {
            get
            {
                if (_pathCache is not null)
                    return _pathCache;

                string path = this.Part.ParentCategory.Name;
                var c = Part.ParentCategory.ParentCategory;
                while (c is not null)
                {
                    path = $"{c.Name}/{path}";
                    c = c.ParentCategory;
                }
                _pathCache = path;
                return path;
            }
        }

        // Included so the KiCad symbol name drop down has a source
        public KiCadSymbolLibrary? SelectedKiCadSymbolLibrary
        {
            // Have to do ! as FirstOrDefault needs to think kSLVM could be null in order for me to return null
            get { return Part.ParentLibrary.KiCadSymbolLibraries.FirstOrDefault(kSLVM => kSLVM!.Nickname == Part.SymbolLibraryName, null); }
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

            ParameterAccessor = new(this);
            FootprintLibraryNameAccessor = new(this);
            FootprintNameAccessor = new(this);
            SelectedFootprintLibraryAccessor = new(this);

            Part.FootprintPairs.CollectionChanged += Part_FootprintPairs_CollectionChanged;
            Part.ParameterValues.CollectionChanged += Part_ParameterValues_CollectionChanged;
        }

        public void Unsubscribe()
        {
            Part.FootprintPairs.CollectionChanged -= Part_FootprintPairs_CollectionChanged;
            Part.ParameterValues.CollectionChanged -= Part_ParameterValues_CollectionChanged;
        }

        private void Part_FootprintPairs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            InvokePropertyChanged(nameof(this.FootprintCount));

            // This is needed to update the table's existing cells
            FootprintLibraryNameAccessor.InvokePropertyChanged("Item[]");
            FootprintNameAccessor.InvokePropertyChanged("Item[]");
        }

        private void Part_ParameterValues_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ParameterAccessor.InvokePropertyChanged("Item[]");
        }

        public void InvokePropertyChanged_Path()
        {
            // Need to remember to void the Path cache
            this._pathCache = null;
            InvokePropertyChanged(nameof(this.Path));
        }

        #region Commands



        #endregion Commands

    }

    public class ParameterAccessor : NotifyObject
    {
        public readonly PartVM OwnerPartVM;

        #region Notify Properties

        public string? this[string parameterName]
        {
            get
            {
                if (OwnerPartVM.Part.ParameterValues.TryGetValue(parameterName, out string? val))
                    return val;
                else
                    return null;
            }
            set
            {
                if (value is not null)
                {
                    if (OwnerPartVM.Part.ParameterValues.TryGetValue(parameterName, out string? s))
                    {
                        if (s != value)
                        {
                            OwnerPartVM.Part.ParameterValues[parameterName] = value;

                            // Don't need to InvokePropertyChanged for this as changing OwnerPartVM.Part.ParameterValues
                            // already causes that
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
                        OwnerPartVM.Part.FootprintPairs[index] = (value, OwnerPartVM.Part.FootprintPairs[index].Item2);

                        // Don't need to InvokePropertyChanged for this or FootprintNameAccessor as changing OwnerPartVM.Part.FootprintPairs
                        // already causes that
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

                        // Don't need to InvokePropertyChanged for this as changing OwnerPartVM.Part.FootprintPairs
                        // already causes that
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
