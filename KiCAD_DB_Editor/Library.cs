using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using System.Xml.Linq;

namespace KiCAD_DB_Editor
{
    public class Library : NotifyObject
    {
        public static bool CheckNameValid(string name)
        {
            string trimmedNewLibraryName = name.Trim();
            return trimmedNewLibraryName != "";
        }

        private string? _name = null;
        [JsonPropertyName("name")]
        public string Name
        {
            get { Debug.Assert(_name is not null); return _name; }
            set
            {
                if (_name != value)
                {
                    if (Library.CheckNameValid(value))
                    {
                        _name = value;

                        InvokePropertyChanged();
                    }
                    else
                    {
                        throw new ArgumentException("Must not be an empty string");
                    }
                }
            }
        }

        private string? _description = null;
        [JsonPropertyName("description")]
        public string Description
        {
            get { Debug.Assert(_description is not null); return _description; }
            set
            {
                if (_description != value)
                {
                    _description = value;

                    InvokePropertyChanged();
                }
            }
        }

        private LibrarySource? _source = null;
        [JsonPropertyName("source")]
        public LibrarySource Source
        {
            get { Debug.Assert(_source is not null); return _source; }
            set
            {
                if (_source != value)
                {
                    _source = value;

                    InvokePropertyChanged();
                }
            }
        }

        private Category? _selectedCategory = null;
        [JsonIgnore]
        public Category? SelectedCategory
        {
            get { return _selectedCategory; }
            set
            {
                if (_selectedCategory != value)
                {
                    _selectedCategory = value;

                    InvokePropertyChanged();
                }
            }
        }

        private ObservableCollection<Category>? _categories = null;
        [JsonIgnore]
        public ObservableCollection<Category> Categories
        {
            get { Debug.Assert(_categories is not null); return _categories; }
            set
            {
                if (_categories != value)
                {
                    if (_categories is not null)
                        _categories.CollectionChanged -= _categories_CollectionChanged;
                    _categories = value;
                    _categories.CollectionChanged += _categories_CollectionChanged;
                    _categories_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

                    InvokePropertyChanged();
                }
            }
        }
        [JsonPropertyName("categories")]
        public Category[] CategoriesEncapsulated
        {
            get { return Categories.OrderBy(c => c.Name).ToArray(); }
            set { Categories = new ObservableCollection<Category>(value.OrderBy(c => c.Name)); }
        }

        private void _categories_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
        }

        private string? _newCategoryName = null;
        [JsonIgnore]
        public string NewCategoryName
        {
            get { Debug.Assert(_newCategoryName is not null); return _newCategoryName; }
            set
            {
                if (_newCategoryName != value)
                {
                    _newCategoryName = value;

                    InvokePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Exists only to get the WPF designer to believe I can use this object as DataContext
        /// </summary>
        public Library()
        {
            Name = "<Library Name>";
            Description = "";
            Source = new LibrarySource();
            NewCategoryName = ""; // Exists for binding to textbox so must start as not-null
            Categories = new ObservableCollection<Category>();
        }

        public Library(string name) : this()
        {
            Name = name;
        }

        public override string ToString()
        {
            return $"{Name} - {Description}";
        }

        public void NewCategory()
        {
            string newCategoryName;
            if (Category.CheckNameValid(NewCategoryName))
                newCategoryName = NewCategoryName;
            else
            {
                const string newCategoryNamePrefix = $"Category ";
                const string regexPattern = @$"^{newCategoryNamePrefix}\d+$";

                int currentMax = Categories.Where(l => Regex.IsMatch(l.Name, regexPattern))
                    .Select(l => int.Parse(l.Name.Remove(0, newCategoryNamePrefix.Length)))
                    .DefaultIfEmpty()
                    .Max();

                newCategoryName = $"{newCategoryNamePrefix}{currentMax + 1}";
            }

            int loc = 0;
            while (loc < Categories.Count && Categories[loc].Name.CompareTo(newCategoryName) < 0) loc++;
            Categories.Insert(loc, new(this, newCategoryName));
        }

        public void DeleteCategory(Category category)
        {
            Categories.Remove(category);
        }

        public void ImportFromKiCADDBL(string filePath)
        {
            KiCADDBL kiCADDBL = KiCADDBL.FromFile(filePath);
            try
            {
                if (kiCADDBL.Name is not null) Name = kiCADDBL.Name;
            }
            catch (ArgumentException) { }
            if (kiCADDBL.Description is not null) Description = kiCADDBL.Description;
            if (kiCADDBL.Source is not null) Source = new LibrarySource(kiCADDBL.Source);

            var newCategories = kiCADDBL.Libraries?.Select(kL => new Category(this, kL));
            if (newCategories is not null && newCategories.Any())
                CategoriesEncapsulated = newCategories.ToArray();
        }

        public void ExportToKiCADDBLFile(string filePath)
        {
            KiCADDBL kiCADDBL = new(this);
            kiCADDBL.SaveToFile(filePath);
        }
    }
}
