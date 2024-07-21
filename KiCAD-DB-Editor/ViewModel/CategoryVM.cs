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
    public class CategoryVM : TreeViewItemVM, IComparable<FolderVM>
    {
        public readonly Model.Category Category;

        #region Notify Properties

        public override Color TreeViewIconColour { get { return Colors.Blue; } }
        public override TreeViewItemVM[] TreeViewItemVMs
        {
            get { return Array.Empty<TreeViewItemVM>(); }
        }

        public override string Name
        {
            get { return Category.Name; }
            set
            {
                if (Category.Name != value)
                {
                    Category.Name = value;
                    InvokePropertyChanged();
                }
            }
        }

        #endregion Notify Properties

        public CategoryVM(ViewModel.FolderVM? parentFolderVM, Model.Category category)
        {
            // Link model
            Category = category;

            ParentFolderVM = parentFolderVM;
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
