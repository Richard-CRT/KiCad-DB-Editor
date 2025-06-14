using KiCAD_DB_Editor.Model;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace KiCAD_DB_Editor.ViewModel
{
    public class PartVM : NotifyObject
    {
        // Duplicated between LibraryVM and CategoryVM so we move the checks here
        public static bool AddFootprintCommandCanExecute(IEnumerable<PartVM> partVMsToAddFootprintTo)
        {
            return partVMsToAddFootprintTo.Count() > 0;
        }

        // Duplicated between LibraryVM and CategoryVM so we move the checks here
        public static void AddFootprintCommandExecuted(IEnumerable<PartVM> partVMsToAddFootprintTo)
        {
            Debug.Assert(partVMsToAddFootprintTo.Count() > 0);
            foreach (PartVM pVM in partVMsToAddFootprintTo)
                pVM.AddFootprint();
        }

        // Duplicated between LibraryVM and CategoryVM so we move the checks here
        public static bool RemoveFootprintCommandCanExecute(IEnumerable<PartVM> partVMsToRemoveFootprintsFrom)
        {
            return partVMsToRemoveFootprintsFrom.Count() > 0 && partVMsToRemoveFootprintsFrom.All(pVM => pVM.FootprintCount > 0);
        }

        // Duplicated between LibraryVM and CategoryVM so we move the checks here
        public static void RemoveFootprintCommandExecuted(IEnumerable<PartVM> partVMsToRemoveFootprintsFrom)
        {
            Debug.Assert(partVMsToRemoveFootprintsFrom.Count() > 0);
            foreach (PartVM pVM in partVMsToRemoveFootprintsFrom)
                pVM.RemoveFootprint();
        }

        // ======================================================================

        public Part Part { get; }

        #region Notify Properties

        public string Path
        {
            get
            {
                string path = Part.ParentCategory.Name;
                var c = Part.ParentCategory;
                while (c.ParentCategory is not null)
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
                    InvokePropertyChanged();

                    // Doesn't seem to be technically required as the bindings for the ComboBoxes I'm designing this for only load
                    // when the cells are edited, but if not then I'd need to do this to prompt the ComboBoxes to refetch the value
                    // On future investigation, it's clear that I can't switch to a system where the ComboBoxes are persistent. WPF is
                    // weird: when I clear the SelectedKiCADSymbolLibraryVM, the available items in the symbol name should be blank
                    // and it does do this, but if the current text is one of those items, it will get cleared, which is not what
                    // I want at all
                    InvokePropertyChanged(nameof(this.SelectedKiCADSymbolLibrary));

                    // Changing the selected symbol library should prompt clearing the symbol name
                    SymbolName = "";
                }
            }
        }

        // Included so the KiCAD symbol name drop down has a source
        public KiCADSymbolLibrary? SelectedKiCADSymbolLibrary
        {
            // Have to do ! as FirstOrDefault needs to think kSLVM could be null in order for me to return null
            get { return Part.ParentLibrary.KiCADSymbolLibraries.FirstOrDefault(kSLVM => kSLVM!.Nickname == SymbolLibraryName, null); }
        }

        public string SymbolName
        {
            get { return Part.SymbolName; }
            set { if (Part.SymbolName != value) { Part.SymbolName = value; InvokePropertyChanged(); } }
        }

        public int FootprintCount
        {
            get { return Part.FootprintLibraryNames.Count; }
        }

        #endregion Notify Properties

        public PartVM(Model.Part part)
        {
            // Link model
            Part = part;

            ParameterAccessor = new(this);
            FootprintLibraryNameAccessor = new(this);
            FootprintNameAccessor = new(this);
            SelectedFootprintLibraryVMAccessor = new(this);
        }

        public void InvokePropertyChanged_Path()
        {
            InvokePropertyChanged(nameof(this.Path));
        }

        public void AddFootprint()
        {
            // Always needs to be done in tandem
            Part.FootprintLibraryNames.Add("");
            Part.FootprintNames.Add("");

            // Have to do this one to tell the table it might have to redo its columns
            InvokePropertyChanged(nameof(FootprintCount));

            // These 2 are needed to update the table's existing cells
            FootprintNameAccessor.InvokePropertyChanged("Item[]");
            FootprintLibraryNameAccessor.InvokePropertyChanged("Item[]");
        }

        public void RemoveFootprint()
        {
            // Always needs to be done in tandem
            Part.FootprintLibraryNames.RemoveAt(Part.FootprintLibraryNames.Count - 1);
            Part.FootprintNames.RemoveAt(Part.FootprintNames.Count - 1);

            // Have to do this one to tell the table it might have to redo its columns
            InvokePropertyChanged(nameof(FootprintCount));

            // These 2 are needed to update the table's existing cells
            FootprintNameAccessor.InvokePropertyChanged("Item[]");
            FootprintLibraryNameAccessor.InvokePropertyChanged("Item[]");
        }

        #region Commands



        #endregion Commands

    }
}
