using KiCAD_DB_Editor.Commands;
using KiCAD_DB_Editor.Exceptions;
using KiCAD_DB_Editor.Model;
using KiCAD_DB_Editor.View.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor.ViewModel
{
    public class KiCADSymbolLibraryVM : NotifyObject
    {
        public readonly Model.KiCADSymbolLibrary KiCADSymbolLibrary;
        public readonly LibraryVM ParentLibraryVM;

        #region Notify Properties

        public string Nickname
        {
            get { return KiCADSymbolLibrary.Nickname; }
            set
            {
                if (KiCADSymbolLibrary.Nickname != value)
                {
                    if (value.Length == 0)
                        throw new Exceptions.ArgumentValidationException("Proposed name invalid");

                    KiCADSymbolLibrary.Nickname = value;
                    InvokePropertyChanged();
                }
            }
        }

        public string RelativePath
        {
            get { return KiCADSymbolLibrary.RelativePath; }
            set
            {
                if (KiCADSymbolLibrary.RelativePath != value)
                {
                    if (value.Length == 0)
                        throw new Exceptions.ArgumentValidationException("Proposed name invalid");

                    KiCADSymbolLibrary.RelativePath = value;
                    InvokePropertyChanged();
                }
            }
        }

        // Do not initialise here, do in constructor to link collection changed
        private ObservableCollectionEx<string> _kicadSymbolNames;
        public ObservableCollectionEx<string> KiCADSymbolNames
        {
            get { return _kicadSymbolNames; }
            set
            {
                if (_kicadSymbolNames != value)
                {
                    _kicadSymbolNames = value;
                    InvokePropertyChanged();
                }
            }
        }

        #endregion Notify Properties

        public KiCADSymbolLibraryVM(LibraryVM parentLibraryVM, KiCADSymbolLibrary kiCADSymbolLibrary)
        {
            ParentLibraryVM = parentLibraryVM;

            // Link model
            KiCADSymbolLibrary = kiCADSymbolLibrary;

            // Parse to produce symbol names
            // Special case while developing
            if (kiCADSymbolLibrary.Nickname == "LIB_GENERIC_CAPACITORS")
                KiCADSymbolNames = new() { "aaaa", "bbbb", "cccc" };
            else
                KiCADSymbolNames = new() { "dddd", "eeee", "ffff" };
            Debug.Assert(_kicadSymbolNames is not null);
        }

        public void ParseKiCADSymbolNames()
        {
            // Need to parse the symbols from the provided library
            string absolutePath = Path.Combine(ParentLibraryVM.Library.ProjectDirectoryPath, KiCADSymbolLibrary.RelativePath);
        }

        #region Commands


        #endregion Commands
    }
}
