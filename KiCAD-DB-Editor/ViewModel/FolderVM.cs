using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Net.Http.Headers;
using System.Windows.Data;
using System.ComponentModel;
using Microsoft.VisualBasic;
using KiCAD_DB_Editor.Commands;
using KiCAD_DB_Editor.View;
using KiCAD_DB_Editor.Exceptions;
using KiCAD_DB_Editor.Model;
using KiCAD_DB_Editor.View.Dialogs;
using System.Security.Cryptography;
using System.Windows.Media;

namespace KiCAD_DB_Editor.ViewModel
{
    public class FolderVM : TreeViewItemVM, IComparable<FolderVM>
    {
        public readonly Model.Folder Folder;

        #region Notify Properties

        // Do not initialise here, do in constructor to link collection changed
        private ObservableCollectionEx<FolderVM> _folderVMs;
        public ObservableCollectionEx<FolderVM> FolderVMs
        {
            get { return _folderVMs; }
            set
            {
                if (_folderVMs != value)
                {
                    if (_folderVMs is not null)
                        _folderVMs.CollectionChanged -= _folderVMs_CollectionChanged;
                    _folderVMs = value;
                    _folderVMs.CollectionChanged += _folderVMs_CollectionChanged;

                    InvokePropertyChanged();
                    _folderVMs_CollectionChanged(this, new (NotifyCollectionChangedAction.Reset));
                }
            }
        }

        private void _folderVMs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            InvokePropertyChanged(nameof(TreeViewItemVMs));
        }

        // Do not initialise here, do in constructor to link collection changed
        private ObservableCollectionEx<CategoryVM> _categoryVMs;
        public ObservableCollectionEx<CategoryVM> CategoryVMs
        {
            get { return _categoryVMs; }
            set
            {
                if (_categoryVMs != value)
                {
                    _categoryVMs = value;

                    InvokePropertyChanged();
                }
            }
        }

        public override Color TreeViewIconColour { get { return Colors.Red; } }
        public override TreeViewItemVM[] TreeViewItemVMs
        {
            get { return FolderVMs.Select(fVM => (TreeViewItemVM)fVM).Concat(CategoryVMs.Select(cVM => (TreeViewItemVM)cVM)).ToArray(); }
        }

        public override string Name
        {
            get { return Folder.Name; }
            set
            {
                if (Folder.Name != value)
                {
                    Folder.Name = value;
                    InvokePropertyChanged();
                }
            }
        }

        #endregion Notify Properties

        public FolderVM(ViewModel.FolderVM? parentFolderVM, Model.Folder folder)
        {
            // Link model
            Folder = folder;

            ParentFolderVM = parentFolderVM;

            // Initialise collection with events
            FolderVMs = new(folder.Folders.OrderBy(f => f.Name).Select(f => new FolderVM(this, f)));
            Debug.Assert(_folderVMs is not null);
            CategoryVMs = new(folder.Categories.OrderBy(c => c.Name).Select(c => new CategoryVM(this, c)));
            Debug.Assert(_categoryVMs is not null);
        }

        public int CompareTo(FolderVM? other)
        {
            if (other is null)
                return 1;
            else
                return this.Name.CompareTo(other.Name);
        }


        #region Commands

        #endregion Commands


    }
}
