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
                o = (Project?)JsonSerializer.Deserialize(jsonString, typeof(Project));

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
        [JsonPropertyName("libraries")]
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
                    InvokePropertyChanged(nameof(NewLibraryNameValid));
                }
            }
        }
        [JsonIgnore]
        public bool NewLibraryNameValid
        {
            get
            {
                string trimmedNewLibraryName = NewLibraryName.Trim();
                return
                    trimmedNewLibraryName != "" &&
                    !Libraries.Any(l => l.Name.ToLower() == trimmedNewLibraryName.ToLower());
            }
        }

        public Project()
        {
            NewLibraryName = ""; // Exists for binding to textbox so must start as not-null
            Libraries = new ObservableCollection<Library>();
        }

        public void NewLibrary(string description)
        {
            if (NewLibraryNameValid)
                Libraries.Add(new(NewLibraryName, description));
        }

        public void SaveToFile(string filePath)
        {
            File.WriteAllText(filePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
