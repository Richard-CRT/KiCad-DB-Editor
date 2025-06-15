using KiCAD_DB_Editor.Model.Json;
using KiCAD_DB_Editor.ViewModel;
using KiCAD_DB_Editor.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor.Model
{
    public class KiCADFootprintLibrary : NotifyObject
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

                    this.ParseKiCADFootprintNames();
                }
            }
        }

        // No setter, to prevent the VM needing to listening PropertyChanged events
        private ObservableCollectionEx<string> _kicadFootprintNames;
        public ObservableCollectionEx<string> KiCADFootprintNames
        {
            get { return _kicadFootprintNames; }
        }

        #endregion Notify Properties

        public void ParseKiCADFootprintNames()
        {
            // Need to parse the footprints from the provided path
            string absolutePath = Path.Combine(ParentLibrary.ProjectDirectoryPath, RelativePath);
            if (Directory.Exists(absolutePath))
            {
                var absoluteFilePaths = Directory.GetFiles(absolutePath, "*.kicad_mod", SearchOption.TopDirectoryOnly);

                KiCADFootprintNames.Clear();
                foreach (string absoluteFilePath in absoluteFilePaths)
                {
                    string fileText = File.ReadAllText(absoluteFilePath);
                    SExpressionToken kiCADFootprintSExpToken = SExpressionToken.FromString(fileText);
                    if (kiCADFootprintSExpToken.Name != "footprint")
                        throw new FormatException($"Top level S-Expression in provided file is not a KiCAD footprint: {absoluteFilePath}");
                    KiCADFootprintNames.Add(kiCADFootprintSExpToken.Attributes[0][1..^1]);
                }
            }
            else
                KiCADFootprintNames.Clear();
        }

        public KiCADFootprintLibrary(JsonKiCADFootprintLibrary jsonKiCADFootprintLibrary, Library parentLibrary)
        {
            // Must be initialised before RelativePath
            _kicadFootprintNames = new();

            _parentLibrary = parentLibrary;
            Nickname = jsonKiCADFootprintLibrary.Nickname;
            RelativePath = jsonKiCADFootprintLibrary.RelativePath; // Triggers parse
        }

        public KiCADFootprintLibrary(string nickname, string relativePath, Library parentLibrary)
        {
            // Must be initialised before RelativePath
            _kicadFootprintNames = new();

            _parentLibrary = parentLibrary;
            Nickname = nickname;
            RelativePath = relativePath; // Triggers parse
        }
    }
}
