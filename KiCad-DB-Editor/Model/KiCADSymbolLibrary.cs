using KiCad_DB_Editor.Model.Json;
using KiCad_DB_Editor.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCad_DB_Editor.Model
{
    public class KiCadSymbolLibrary : NotifyObject
    {
        #region Notify Properties

        // No setter, to prevent the VM needing to listening PropertyChanged events
        private Library _parentLibrary;
        public Library ParentLibrary
        {
            get { return _parentLibrary; }
        }

        private string _nickname = "";
        public string Nickname
        {
            get { return _nickname; }
            set
            {
                if (_nickname != value)
                {
                    if (value.Length == 0)
                        throw new Exceptions.ArgumentValidationException("Proposed name invalid");

                    _nickname = value;
                    InvokePropertyChanged();
                }
            }
        }

        private string _relativePath = "";
        public string RelativePath
        {
            get { return _relativePath; }
            set
            {
                if (_relativePath != value)
                {
                    if (value.Length == 0)
                        throw new Exceptions.ArgumentValidationException("Proposed name invalid");

                    _relativePath = value;
                    InvokePropertyChanged();

                    this.ParseKiCadSymbolNames();
                }
            }
        }

        // No setter, to prevent the VM needing to listening PropertyChanged events
        private ObservableCollectionEx<string> _kiCadSymbolNames;
        public ObservableCollectionEx<string> KiCadSymbolNames
        {
            get { return _kiCadSymbolNames; }
        }

        #endregion Notify Properties

        public void ParseKiCadSymbolNames()
        {
            // Need to parse the symbols from the provided library
            string absolutePath = Path.Combine(ParentLibrary.ProjectDirectoryPath, RelativePath);
            absolutePath = (new Uri(absolutePath)).AbsolutePath;
            if (File.Exists(absolutePath))
            {
                string fileText = File.ReadAllText(absolutePath);
                SExpressionToken kiCadSymbolLibSExpToken = SExpressionToken.FromString(fileText);
                if (kiCadSymbolLibSExpToken.Name != "kicad_symbol_lib")
                    throw new FormatException($"Top level S-Expression in provided file is not a KiCad symbol library: {absolutePath}");
                var kiCadSymbolSExpTokens = kiCadSymbolLibSExpToken.SubTokens.Where(sT => sT.Name == "symbol");
                KiCadSymbolNames.Clear();
                KiCadSymbolNames.AddRange(kiCadSymbolSExpTokens.Select(sT => sT.Attributes[0][1..^1]));
            }
            else
                KiCadSymbolNames.Clear();
        }

        public KiCadSymbolLibrary(JsonKiCadSymbolLibrary jsonKiCadSymbolLibrary, Library parentLibrary)
        {
            // Must be initialised before RelativePath
            _kiCadSymbolNames = new();

            _parentLibrary = parentLibrary;
            Nickname = jsonKiCadSymbolLibrary.Nickname;
            RelativePath = jsonKiCadSymbolLibrary.RelativePath; // Triggers parse
        }

        public KiCadSymbolLibrary(string nickname, string relativePath, Library parentLibrary)
        {
            // Must be initialised before RelativePath
            _kiCadSymbolNames = new();

            _parentLibrary = parentLibrary;
            Nickname = nickname;
            RelativePath = relativePath; // Triggers parse
        }
    }
}
