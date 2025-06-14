using KiCAD_DB_Editor.ViewModel;
using KiCAD_DB_Editor.ViewModel.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor.Model
{
    public class KiCADSymbolLibrary : NotifyObject
    {
        #region Notify Properties

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
                }
            }
        }

        // No setter, to prevent the VM needing to listening PropertyChanged events
        private ObservableCollectionEx<string> _kicadSymbolNames;
        public ObservableCollectionEx<string> KiCADSymbolNames
        {
            get { return _kicadSymbolNames; }
        }

        #endregion Notify Properties

        public KiCADSymbolLibrary(string nickname, string relativePath)
        {
            Nickname = nickname;
            RelativePath = relativePath;
        }

        public void ParseKiCADSymbolNames()
        {
            // Need to parse the symbols from the provided library
            string absolutePath = Path.Combine(ParentLibraryVM.Library.ProjectDirectoryPath, KiCADSymbolLibrary.RelativePath);
            if (File.Exists(absolutePath))
            {
                string fileText = File.ReadAllText(absolutePath);
                SExpressionToken kiCADSymbolLibSExpToken = SExpressionToken.FromString(fileText);
                if (kiCADSymbolLibSExpToken.Name != "kicad_symbol_lib")
                    throw new FormatException($"Top level S-Expression in provided file is not a KiCAD symbol library: {absolutePath}");
                var kiCADSymbolSExpTokens = kiCADSymbolLibSExpToken.SubTokens.Where(sT => sT.Name == "symbol");
                KiCADSymbolNames = new(kiCADSymbolSExpTokens.Select(sT => sT.Attributes[0][1..^1]));
            }
            else
                KiCADSymbolNames = new();
        }
    }
}
