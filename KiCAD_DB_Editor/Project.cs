using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace KiCAD_DB_Editor
{
    public class Project : NotifyObject
    {
        public static Project FromFile(string filePath)
        {
            Project project;
            try
            {
                var jsonString = File.ReadAllText(filePath);

                Project? o;
                o = (Project?)JsonSerializer.Deserialize(jsonString, typeof(Project), new JsonSerializerOptions {ReferenceHandler = ReferenceHandler.Preserve });

                if (o is null) throw new ArgumentNullException("Project is null");

                project = (Project)o!;
            }
            catch (FileNotFoundException)
            {
                throw;
            }

            return project;
        }


        // ============================================================================================
        // ============================================================================================

        private Library? _selectedLibrary = null;
        [JsonIgnore]
        public Library? SelectedLibrary
        {
            get { return _selectedLibrary; }
            set
            {
                if (_selectedLibrary != value)
                {
                    _selectedLibrary = value;

                    InvokePropertyChanged();
                }
            }
        }

        private ObservableCollection<Library>? _libraries = null;
        [JsonIgnore]
        public ObservableCollection<Library> Libraries
        {
            get { Debug.Assert(_libraries is not null); return _libraries; }
            set
            {
                if (_libraries != value)
                {
                    if (_libraries is not null)
                        _libraries.CollectionChanged -= _libraries_CollectionChanged;
                    _libraries = value;
                    _libraries.CollectionChanged += _libraries_CollectionChanged;
                    _libraries_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

                    InvokePropertyChanged();
                }
            }
        }
        [JsonPropertyName("libraries")]
        public List<Library> LibrariesEncapsulated
        {
            get { return Libraries.OrderBy(l => l.Name).ToList(); }
            set { Libraries = new ObservableCollection<Library>(value.OrderBy(l => l.Name)); }
        }

        private void _libraries_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
        }

        private string? _newLibraryName = null;
        [JsonIgnore]
        public string NewLibraryName
        {
            get { Debug.Assert(_newLibraryName is not null); return _newLibraryName; }
            set
            {
                if (_newLibraryName != value)
                {
                    _newLibraryName = value;

                    InvokePropertyChanged();
                }
            }
        }

        public Project()
        {
            NewLibraryName = ""; // Exists for binding to textbox so must start as not-null
            Libraries = new ObservableCollection<Library>();
        }

        public Library NewLibrary()
        {
            Library newLibrary;

            if (Library.CheckNameValid(NewLibraryName))
                newLibrary = new(NewLibraryName);
            else
            {
                const string newLibraryNamePrefix = $"Library ";
                const string regexPattern = @$"^{newLibraryNamePrefix}\d+$";

                int currentMax = Libraries.Where(l => Regex.IsMatch(l.Name, regexPattern))
                    .Select(l => int.Parse(l.Name.Remove(0, newLibraryNamePrefix.Length)))
                    .DefaultIfEmpty()
                    .Max();

                string newLibraryName = $"{newLibraryNamePrefix}{currentMax + 1}";

                newLibrary = new(newLibraryName);
            }

            Libraries.Add(newLibrary);
            return newLibrary;
        }

        public Library NewLibrary(string importFilePath)
        {
            Library newLibrary = NewLibrary();
            newLibrary.ImportFromKiCADDBL(importFilePath);
            return newLibrary;
        }

        public void DeleteLibrary(Library library)
        {
            Libraries.Remove(library);
        }

        public void SaveToFile(string filePath)
        {
            File.WriteAllText(filePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true, ReferenceHandler = ReferenceHandler.Preserve }));
        }
    }
}
