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
            Project o;
            try
            {
                var jsonString = File.ReadAllText(filePath);

                Project? project;
                project = (Project?)JsonSerializer.Deserialize(jsonString, typeof(Project));

                if (project is null) throw new ArgumentNullException("Project is null");

                o = (Project)project!;
            }
            catch (FileNotFoundException)
            {
                throw;
            }

            return o;
        }


        // ============================================================================================
        // ============================================================================================


        private string? _projectPath;
        public string ProjectPath
        {
            get { Debug.Assert(_projectPath is not null); return _projectPath; }
            set
            {
                if (_projectPath != value)
                {
                    _projectPath = value;

                    InvokePropertyChanged();

                    ScanForLibraries();
                }
            }
        }

        private ObservableCollection<Library>? _libraries = null;
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
            ScanForLibraries();
        }

        private ObservableCollection<string>? _unincludedLibraries = null;
        [JsonIgnore]
        public ObservableCollection<string> UnincludedLibraries
        {
            get { Debug.Assert(_unincludedLibraries is not null); return _unincludedLibraries; }
            set
            {
                if (_unincludedLibraries != value)
                {
                    _unincludedLibraries = value;

                    InvokePropertyChanged();
                }
            }
        }

        private string? _selectedUnincludedLibrary = null;
        [JsonIgnore]
        public string? SelectedUnincludedLibrary
        {
            get { return _selectedUnincludedLibrary; }
            set
            {
                if (_selectedUnincludedLibrary != value)
                {
                    _selectedUnincludedLibrary = value;

                    InvokePropertyChanged();
                }
            }
        }

        public Project()
        {
            ProjectPath = "";
            Libraries = new ObservableCollection<Library>();
        }

        public Project(string filepath)
        {
            ProjectPath = "";
            Libraries = new ObservableCollection<Library>();
        }

        public void ScanForLibraries()
        {
            if (Directory.Exists(ProjectPath))
            {
                HashSet<string> includedLibraryPaths = new HashSet<string>(Libraries.Select(l => l.DblFilePath));
                UnincludedLibraries = new ObservableCollection<string>(
                    Directory.GetFiles(ProjectPath, "*.kicad_dbl")
                    .Select(p => Path.GetRelativePath(ProjectPath, p))
                    .Where(p => !includedLibraryPaths.Contains(p))
                    );
            }
            else
            {
                UnincludedLibraries = new ObservableCollection<string>();
            }
        }

        public void IncludeSelectedUnincludedLibrary()
        {
            if (SelectedUnincludedLibrary is not null)
            {
                Libraries.Add(new Library(SelectedUnincludedLibrary));
            }
        }

        public void SaveToFile(string filePath)
        {
            File.WriteAllText(filePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
