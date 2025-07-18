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
    public class KiCadFootprintLibrary : NotifyObject
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

                    this.ParseKiCadFootprintNames();
                }
            }
        }

        // No setter, to prevent the VM needing to listening PropertyChanged events
        private ObservableCollectionEx<string> _kiCadFootprintNames;
        public ObservableCollectionEx<string> KiCadFootprintNames
        {
            get { return _kiCadFootprintNames; }
        }

        #endregion Notify Properties

        public void ParseKiCadFootprintNames()
        {
            // Need to parse the footprints from the provided path
            string absolutePath = Path.Combine(ParentLibrary.ProjectDirectoryPath, RelativePath);
            absolutePath = (new Uri(absolutePath)).AbsolutePath;
            if (Directory.Exists(absolutePath))
            {
                var absoluteFilePaths = Directory.GetFiles(absolutePath, "*.kicad_mod", SearchOption.TopDirectoryOnly);

                KiCadFootprintNames.Clear();
                foreach (string absoluteFilePath in absoluteFilePaths)
                {
                    string fileText = File.ReadAllText(absoluteFilePath);
                    SExpressionToken kiCadFootprintSExpToken = new(fileText);
                    if (kiCadFootprintSExpToken.Name != "footprint")
                        throw new FormatException($"Top level S-Expression in provided file is not a KiCad footprint: {absoluteFilePath}");
                    KiCadFootprintNames.Add(kiCadFootprintSExpToken.Attributes[0][1..^1]);
                }
            }
            else
                KiCadFootprintNames.Clear();
        }

        public KiCadFootprintLibrary(JsonKiCadFootprintLibrary jsonKiCadFootprintLibrary, Library parentLibrary)
        {
            // Must be initialised before RelativePath
            _kiCadFootprintNames = new();

            _parentLibrary = parentLibrary;
            Nickname = jsonKiCadFootprintLibrary.Nickname;
            RelativePath = jsonKiCadFootprintLibrary.RelativePath; // Triggers parse
        }

        public KiCadFootprintLibrary(string nickname, string relativePath, Library parentLibrary)
        {
            // Must be initialised before RelativePath
            _kiCadFootprintNames = new();

            _parentLibrary = parentLibrary;
            Nickname = nickname;
            RelativePath = relativePath; // Triggers parse
        }
    }
}
