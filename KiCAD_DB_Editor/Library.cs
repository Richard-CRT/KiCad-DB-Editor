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
            Description = "<Library SymbolDescriptionTableColumnName>";
            Source = new LibrarySource();
            NewCategoryName = ""; // Exists for binding to textbox so must start as not-null
            Categories = new ObservableCollection<Category>();
        }

        public Library(string name, string description) : this()
        {
            Name = name;
            Description = description;
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
            Categories.Insert(loc, new(newCategoryName));
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

            var newCategories = kiCADDBL.Libraries?.Select(kL => new Category(kL));
            if (newCategories is not null && newCategories.Any())
                CategoriesEncapsulated = newCategories.ToArray();
        }

        public void ExportToKiCADDBLFile(string filePath)
        {
            KiCADDBL kiCADDBL = new(this);
            kiCADDBL.SaveToFile(filePath);
        }
    }

    public class LibrarySource : NotifyObject
    {
        private string? _type = null;
        [JsonPropertyName("type")]
        public string Type
        {
            get { Debug.Assert(_type is not null); return _type; }
            set
            {
                if (_type != value)
                {
                    _type = value;

                    InvokePropertyChanged();
                }
            }
        }

        private string? _dSN = null;
        [JsonPropertyName("dsn")]
        public string DSN
        {
            get { Debug.Assert(_dSN is not null); return _dSN; }
            set
            {
                if (_dSN != value)
                {
                    _dSN = value;

                    InvokePropertyChanged();
                }
            }
        }

        private string? _username = null;
        [JsonPropertyName("username")]
        public string Username
        {
            get { Debug.Assert(_username is not null); return _username; }
            set
            {
                if (_username != value)
                {
                    _username = value;

                    InvokePropertyChanged();
                }
            }
        }

        private string? _password = null;
        [JsonPropertyName("password")]
        public string Password
        {
            get { Debug.Assert(_password is not null); return _password; }
            set
            {
                if (_password != value)
                {
                    _password = value;

                    InvokePropertyChanged();
                }
            }
        }

        private int? _timeOutSeconds = null;
        [JsonPropertyName("time_out_seconds")]
        public int TimeOutSeconds
        {
            get { Debug.Assert(_timeOutSeconds is not null); return _timeOutSeconds.Value; }
            set
            {
                if (_timeOutSeconds != value)
                {
                    _timeOutSeconds = value;

                    InvokePropertyChanged();
                }
            }
        }

        private string? _connectionString = null;
        [JsonPropertyName("connection_string")]
        public string ConnectionString
        {
            get { Debug.Assert(_connectionString is not null); return _connectionString; }
            set
            {
                if (_connectionString != value)
                {
                    _connectionString = value;

                    InvokePropertyChanged();
                }
            }
        }

        public LibrarySource()
        {
            Type = "odbc";
            DSN = "";
            Username = "";
            Password = "";
            TimeOutSeconds = 2;
            ConnectionString = "";
        }

        public LibrarySource(KiCADDBL_Source kiCADDBL_Source) : this()
        {
            if (kiCADDBL_Source.Type is not null) Type = kiCADDBL_Source.Type;
            if (kiCADDBL_Source.DSN is not null) DSN = kiCADDBL_Source.DSN;
            if (kiCADDBL_Source.Username is not null) Username = kiCADDBL_Source.Username;
            if (kiCADDBL_Source.Password is not null) Password = kiCADDBL_Source.Password;
            if (kiCADDBL_Source.TimeOutSeconds is not null) TimeOutSeconds = kiCADDBL_Source.TimeOutSeconds.Value;
            if (kiCADDBL_Source.ConnectionString is not null) ConnectionString = kiCADDBL_Source.ConnectionString;
        }
    }
}
