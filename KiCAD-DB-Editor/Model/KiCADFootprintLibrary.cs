using KiCAD_DB_Editor.ViewModel;
using KiCAD_DB_Editor.ViewModel.Utilities;
using System;
using System.Collections.Generic;
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
        private ObservableCollectionEx<string> _kicadFootprintNames;
        public ObservableCollectionEx<string> KiCADFootprintNames
        {
            get { return _kicadFootprintNames; }
        }

        #endregion Notify Properties

        public KiCADFootprintLibrary(string nickname, string relativePath)
        {
            Nickname = nickname;
            RelativePath = relativePath;
        }

        public void ParseKiCADFootprintNames()
        {
            // Need to parse the footprints from the provided path
            string absolutePath = Path.Combine(ParentLibraryVM.Library.ProjectDirectoryPath, KiCADFootprintLibrary.RelativePath);
            if (Directory.Exists(absolutePath))
            {
                var absoluteFilePaths = Directory.GetFiles(absolutePath, "*.kicad_mod", SearchOption.TopDirectoryOnly);

                KiCADFootprintNames = new();
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
                KiCADFootprintNames = new();
        }
    }
}
