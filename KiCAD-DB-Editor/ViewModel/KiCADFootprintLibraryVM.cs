using KiCAD_DB_Editor.Commands;
using KiCAD_DB_Editor.Exceptions;
using KiCAD_DB_Editor.Model;
using KiCAD_DB_Editor.View.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor.ViewModel
{
    public class KiCADFootprintLibraryVM : NotifyObject
    {
        public readonly Model.KiCADFootprintLibrary KiCADFootprintLibrary;
        public readonly LibraryVM ParentLibraryVM;

        #region Notify Properties

        public string Nickname
        {
            get { return KiCADFootprintLibrary.Nickname; }
            set
            {
                if (KiCADFootprintLibrary.Nickname != value)
                {
                    if (value.Length == 0)
                        throw new Exceptions.ArgumentValidationException("Proposed name invalid");

                    if (ParentLibraryVM.KiCADFootprintLibraryVMs.Any(p => p.Nickname.ToLower() == value.ToLower()))
                        throw new Exceptions.ArgumentValidationException("Parent already contains KiCAD footprint library with proposed name");

                    KiCADFootprintLibrary.Nickname = value;
                    InvokePropertyChanged();
                }
            }
        }

        public string RelativePath
        {
            get { return KiCADFootprintLibrary.RelativePath; }
            set
            {
                if (KiCADFootprintLibrary.RelativePath != value)
                {
                    if (value.Length == 0)
                        throw new Exceptions.ArgumentValidationException("Proposed name invalid");

                    KiCADFootprintLibrary.RelativePath = value;
                    InvokePropertyChanged();
                }
            }
        }

        // Do not initialise here, do in constructor to link collection changed
        private ObservableCollectionEx<string> _kicadFootprintNames;
        public ObservableCollectionEx<string> KiCADFootprintNames
        {
            get { return _kicadFootprintNames; }
            set
            {
                if (_kicadFootprintNames != value)
                {
                    _kicadFootprintNames = value;
                    InvokePropertyChanged();
                }
            }
        }

        #endregion Notify Properties

        public KiCADFootprintLibraryVM(LibraryVM parentLibraryVM, KiCADFootprintLibrary kiCADFootprintLibrary)
        {
            ParentLibraryVM = parentLibraryVM;

            // Link model
            KiCADFootprintLibrary = kiCADFootprintLibrary;

            // Parse to produce footprint names
            KiCADFootprintNames = new() { "xxxx", "yyyy", "zzzz" };
            Debug.Assert(_kicadFootprintNames is not null);
        }

        #region Commands


        #endregion Commands
    }
}
