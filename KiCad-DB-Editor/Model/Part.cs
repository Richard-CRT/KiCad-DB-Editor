using KiCad_DB_Editor.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCad_DB_Editor.Model
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
            set { if (_partUID != value) { _partUID = value; InvokePropertyChanged(); } }
        }

        private string _description = "";
        public string Description
        {
            get { return _description; }
            set { if (_description != value) { _description = value; InvokePropertyChanged(); } }
        }

        private string _manufacturer = "";
        public string Manufacturer
        {
            get { return _manufacturer; }
            set
            {
                if (_manufacturer != value)
                {
                    _manufacturer = value;
                    InvokePropertyChanged();
                    ParentLibrary.InvokePropertyChanged(nameof(ParentLibrary.AllManufacturers));
                }
            }
        }

        private string _mpn = "";
        public string MPN
        {
            get { return _mpn; }
            set { if (_mpn != value) { _mpn = value; InvokePropertyChanged(); } }
        }

        private string _value = "";
        public string Value
        {
            get { return _value; }
            set { if (_value != value) { _value = value; InvokePropertyChanged(); } }
        }

        private string _datasheet = "";
        public string Datasheet
        {
            get { return _datasheet; }
            set { if (_datasheet != value) { _datasheet = value; InvokePropertyChanged(); } }
        }

        private string _symbolLibraryName = "";
        public string SymbolLibraryName
        {
            get { return _symbolLibraryName; }
            set { if (_symbolLibraryName != value) { _symbolLibraryName = value; InvokePropertyChanged(); } }
        }

        private string _symbolName = "";
        public string SymbolName
        {
            get { return _symbolName; }
            set { if (_symbolName != value) { _symbolName = value; InvokePropertyChanged(); } }
        }

        // No setter, to prevent the VM needing to listening PropertyChanged events
        private ObservableCollectionEx<(string, string)> _footprintPairs;
        public ObservableCollectionEx<(string, string)> FootprintPairs
        {
            get { return _footprintPairs; }
        }

        // No setter, to prevent the VM needing to listening PropertyChanged events
        private ObservableDictionary<string, string> _parameterValues;
        public ObservableDictionary<string, string> ParameterValues
        {
            get { return _parameterValues; }
        }

        private bool _excludeFromBOM = false;
        public bool ExcludeFromBOM
        {
            get { return _excludeFromBOM; }
            set { if (_excludeFromBOM != value) { _excludeFromBOM = value; InvokePropertyChanged(); } }
        }

        private bool _excludeFromBoard = false;
        public bool ExcludeFromBoard
        {
            get { return _excludeFromBoard; }
            set { if (_excludeFromBoard != value) { _excludeFromBoard = value; InvokePropertyChanged(); } }
        }

        private bool _excludeFromSim = false;
        public bool ExcludeFromSim
        {
            get { return _excludeFromSim; }
            set { if (_excludeFromSim != value) { _excludeFromSim = value; InvokePropertyChanged(); } }
        }

        #endregion Notify Properties

        public Part(string partUID, Library parentLibrary, Category parentCategory)
        {
            _parentLibrary = parentLibrary;
            _parentCategory = parentCategory;
            PartUID = partUID;

            _footprintPairs = new();
            _parameterValues = new();
        }

        public void CopyFromPart(Part partToCopy)
        {
            this.Description = partToCopy.Description;
            this.Manufacturer = partToCopy.Manufacturer;
            // Not copying MPN because that's not sensible
            this.Value = partToCopy.Value;
            this.Datasheet = partToCopy.Datasheet;
            this.SymbolLibraryName = partToCopy.SymbolLibraryName;
            this.SymbolName = partToCopy.SymbolName;
            this.ExcludeFromBOM = partToCopy.ExcludeFromBOM;
            this.ExcludeFromBoard = partToCopy.ExcludeFromBoard;
            this.ExcludeFromSim = partToCopy.ExcludeFromSim;

            _footprintPairs = new(partToCopy.FootprintPairs); // Shallow copy because using ValueTuple
            foreach (string parameter in this.ParameterValues.Keys) // Guarantees that we don't add parameters from the partToCopy that don't already exist on this part
                this.ParameterValues[parameter] = partToCopy.ParameterValues[parameter];
        }

        public string Substitute(string input)
        {
            int startSearchIndex = 0;
            int startIndex;
            while ((startIndex = input.IndexOf("${", startSearchIndex)) >= 0)
            {
                int endIndex;
                if ((endIndex = input.IndexOf('}', startIndex + 2)) > startIndex)
                {
                    string substring = input[startIndex..(endIndex + 1)];
                    string parameterName = substring[2..^1].ToLowerInvariant();
                    string? parameter = ParameterValues.Keys.FirstOrDefault(k => k!.Equals(parameterName, StringComparison.InvariantCultureIgnoreCase), null);
                    if (parameter is not null)
                        input = input.Replace(substring, ParameterValues[parameter], StringComparison.InvariantCultureIgnoreCase);
                    else
                        startSearchIndex = endIndex + 1;
                }
            }
            return input;
        }

        public override string ToString()
        {
            return $"Part: {PartUID} {MPN}";
        }
    }
}
