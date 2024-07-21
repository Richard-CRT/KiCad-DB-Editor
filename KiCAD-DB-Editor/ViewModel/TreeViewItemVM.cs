using KiCAD_DB_Editor.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace KiCAD_DB_Editor.ViewModel
{
    public abstract class TreeViewItemVM : NotifyObject
    {
        private ViewModel.FolderVM? _parentFolderVM;
        public ViewModel.FolderVM? ParentFolderVM
        {
            get { return _parentFolderVM; }
            set
            {
                if (_parentFolderVM != value)
                {
                    _parentFolderVM = value;

                    InvokePropertyChanged(nameof(FolderVM.Path));
                }
            }
        }

        #region Notify Properties

        public abstract Color TreeViewIconColour { get; }
        public abstract TreeViewItemVM[] TreeViewItemVMs { get; }

        public abstract string Name { get; set; }

        public string Path
        {
            get { return ParentFolderVM is null ? $"{Name}/" : $"{ParentFolderVM.Path}{Name}/"; }
        }

        #endregion Notify Properties
    }
}
