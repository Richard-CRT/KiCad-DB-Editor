using KiCAD_DB_Editor.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor.Model
{
    public class Part : NotifyObject
    {
        #region Notify Properties

        // No setter, to prevent the VM needing to listening PropertyChanged events
        private Category _parentCategory;
        public Category ParentCategory
        {
            get { return _parentCategory; }
        }

        // No setter, to prevent the VM needing to listening PropertyChanged events
        private Library _parentLibrary;
        public Library ParentLibrary
        {
            get { return _parentLibrary; }
        }

        private string _partUID = "";
        public string PartUID
        {
            get { return _partUID; }
            set { if (_partUID != value) _partUID = value; InvokePropertyChanged(); }
        }

        private string _description = "";
        public string Description
        {
            get { return _description; }
            set { if (_description != value) _description = value; InvokePropertyChanged(); }
        }

        private string _manufacturer = "";
        public string Manufacturer
        {
            get { return _manufacturer; }
            set { if (_manufacturer != value) _manufacturer = value; InvokePropertyChanged(); }
        }

        private string _mpn = "";
        public string MPN
        {
            get { return _mpn; }
            set { if (_mpn != value) _mpn = value; InvokePropertyChanged(); }
        }

        private string _value = "";
        public string Value
        {
            get { return _value; }
            set { if (_value != value) _value = value; InvokePropertyChanged(); }
        }

        private string _datasheet = "";
        public string Datasheet
        {
            get { return _datasheet; }
            set { if (_datasheet != value) _datasheet = value; InvokePropertyChanged(); }
        }

        private string _symbolLibraryName = "";
        public string SymbolLibraryName
        {
            get { return _symbolLibraryName; }
            set { if (_symbolLibraryName != value) _symbolLibraryName = value; InvokePropertyChanged(); }
        }

        private string _symbolName = "";
        public string SymbolName
        {
            get { return _symbolName; }
            set { if (_symbolName != value) _symbolName = value; InvokePropertyChanged(); }
        }

        // No setter, to prevent the VM needing to listening PropertyChanged events
        private ObservableCollectionEx<(string,string)> _footprintPairs;
        public ObservableCollectionEx<(string,string)> FootprintPairs
        {
            get { return _footprintPairs; }
        }

        private bool _excludeFromBOM = false;
        public bool ExcludeFromBOM
        {
            get { return _excludeFromBOM; }
            set { if (_excludeFromBOM != value) _excludeFromBOM = value; InvokePropertyChanged(); }
        }

        private bool _excludeFromBoard = false;
        public bool ExcludeFromBoard
        {
            get { return _excludeFromBoard; }
            set { if (_excludeFromBoard != value) _excludeFromBoard = value; InvokePropertyChanged(); }
        }

        private bool _excludeFromSim = false;
        public bool ExcludeFromSim
        {
            get { return _excludeFromSim; }
            set { if (_excludeFromSim != value) _excludeFromSim = value; InvokePropertyChanged(); }
        }

        #endregion Notify Properties

        public Dictionary<Parameter, string> ParameterValues { get; set; } = new();

        public Part(string partUID, Library parentLibrary, Category parentCategory)
        {
            _parentLibrary = parentLibrary;
            _parentCategory = parentCategory;
            PartUID = partUID;

            // Initialise collection with events
            _footprintPairs = new();
        }

        public override string ToString()
        {
            return $"{PartUID}";
        }
    }
}
