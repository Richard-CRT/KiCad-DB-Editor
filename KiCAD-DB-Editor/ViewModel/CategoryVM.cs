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
using System.Formats.Asn1;

namespace KiCAD_DB_Editor.ViewModel
{
    public class CategoryVM : NotifyObject
    {
        public readonly Model.Category Category;
        public readonly LibraryVM ParentLibraryVM;
        public readonly CategoryVM? ParentCategoryVM;

        #region Notify Properties

        public string Name
        {
            get { return Category.Name; }
            set
            {
                if (Category.Name != value)
                {
                    ObservableCollectionEx<CategoryVM> categoryCollection;
                    if (ParentCategoryVM is null)
                        categoryCollection = ParentLibraryVM.TopLevelCategoryVMs;
                    else
                        categoryCollection = ParentCategoryVM.CategoryVMs;

                    if (categoryCollection.Any(cVM => cVM.Name.ToLower() == value.ToLower()))
                        throw new Exceptions.ArgumentValidationException("Parent already contains category with proposed name");

                    Category.Name = value;
                    InvokePropertyChanged();

                    int oldIndex = categoryCollection.IndexOf(this);
                    int newIndex = 0;
                    for (int i = 0; i < categoryCollection.Count; i++)
                    {
                        CategoryVM compareCategoryVM = categoryCollection[i];
                        if (compareCategoryVM != this)
                        {
                            if (compareCategoryVM.Name.CompareTo(this.Name) > 0)
                                break;
                            newIndex++;
                        }
                    }
                    if (oldIndex != newIndex)
                        categoryCollection.Move(oldIndex, newIndex);
                }
            }
        }

        // Do not initialise here, do in constructor to link collection changed
        private ObservableCollectionEx<CategoryVM> _categoryVMs;
        public ObservableCollectionEx<CategoryVM> CategoryVMs
        {
            get { return _categoryVMs; }
            private set
            {
                if (_categoryVMs != value)
                {
                    if (_categoryVMs is not null)
                        _categoryVMs.CollectionChanged -= _categoryVMs_CollectionChanged;
                    _categoryVMs = value;
                    _categoryVMs.CollectionChanged += _categoryVMs_CollectionChanged;

                    _categoryVMs_CollectionChanged(this, new(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        private void _categoryVMs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Category.Categories = new(this.CategoryVMs.Select(cVM => cVM.Category));

            InvokePropertyChanged(nameof(this.CategoryVMs));
        }

        #endregion Notify Properties

        public CategoryVM(LibraryVM parentLibraryVM, CategoryVM? parentCategoryVM, Model.Category category)
        {
            ParentLibraryVM = parentLibraryVM;
            ParentCategoryVM = parentCategoryVM;

            // Link model
            Category = category;

            // Initialise collection with events
            CategoryVMs = new(category.Categories.OrderBy(c => c.Name).Select(c => new CategoryVM(ParentLibraryVM, this, c)));
            Debug.Assert(_categoryVMs is not null);
        }

        #region Commands

        #endregion Commands
    }
}
