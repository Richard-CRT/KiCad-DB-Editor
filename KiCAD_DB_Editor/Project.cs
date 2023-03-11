using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
        public static readonly Regex s_KeyPatternRegex = new(@"^((#+)[A-Z-_+=!£$%^&*()[\]{}<>:;@,.?]*|[A-Z-_+=!£$%^&*()[\]{}<>:;@,.?]*(#+))$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static string s_GetNextPrimaryKey(string keyPattern, IEnumerable<string> primaryKeys, bool increment)
        {
            List<int> matchingPrimaryKeyInts = new();
            // CMP-###    ->     CMP-(\d\d\d)
            int firstHashIndex = keyPattern.IndexOf('#');
            int lastHashIndex = keyPattern.LastIndexOf('#');
            int numDigits = lastHashIndex - firstHashIndex + 1;
            string hashSubstring = keyPattern.Substring(firstHashIndex, numDigits);
            string modifiedKeyPattern = $"^{keyPattern.Substring(0, firstHashIndex)}({hashSubstring.Replace("#", @"\d")}){keyPattern.Substring(lastHashIndex + 1)}$";
            Regex categorySpecificKeyPatternRegex = new Regex(modifiedKeyPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            foreach (string pK in primaryKeys)
            {
                Match m = categorySpecificKeyPatternRegex.Match(pK);
                if (m.Success)
                    matchingPrimaryKeyInts.Add(int.Parse(m.Groups[1].Value));
            }
            int currentMax = matchingPrimaryKeyInts.DefaultIfEmpty().Max();
            int newPrimaryKeyInt = increment ? currentMax + 1 : currentMax;
            string newPrimaryKey = keyPattern.Replace(hashSubstring, newPrimaryKeyInt.ToString().PadLeft(numDigits, '0'));
            return newPrimaryKey;
        }

        public static Project FromFile(string filePath)
        {
            Project project;
            try
            {
                var jsonString = File.ReadAllText(filePath);

                Project? o;
                o = (Project?)JsonSerializer.Deserialize(jsonString, typeof(Project), new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve });

                if (o is null) throw new ArgumentNullException("Project is null");

                project = (Project)o!;

                project.Libraries.ToList().ForEach(l => l.ParentProject = project);
            }
            catch (FileNotFoundException)
            {
                throw;
            }

            return project;
        }

        // ============================================================================================
        // ============================================================================================

        private string? _projectKeyPattern = null;
        [JsonPropertyName("project_key_pattern")]
        public string ProjectKeyPattern
        {
            get { Debug.Assert(_projectKeyPattern is not null); return _projectKeyPattern; }
            set
            {
                string trimmed = value.Trim();
                if (_projectKeyPattern != trimmed)
                {
                    if (trimmed.Length <= 20 && Project.s_KeyPatternRegex.IsMatch(trimmed))
                    {
                        _projectKeyPattern = trimmed;

                        InvokePropertyChanged();
                    }
                    else
                    {
                        throw new ArgumentException("Must match key pattern");
                    }
                }
            }
        }



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
            ProjectKeyPattern = "CMP-####";
            NewLibraryName = ""; // Exists for binding to textbox so must start as not-null
            Libraries = new ObservableCollection<Library>();
        }

        public Library NewLibrary()
        {
            Library newLibrary;

            if (NewLibraryName != "")
                newLibrary = new(this, NewLibraryName);
            else
            {
                const string newLibraryNamePrefix = $"Library ";
                const string regexPattern = @$"^{newLibraryNamePrefix}\d+$";

                int currentMax = Libraries.Where(l => Regex.IsMatch(l.Name, regexPattern))
                    .Select(l => int.Parse(l.Name.Remove(0, newLibraryNamePrefix.Length)))
                    .DefaultIfEmpty()
                    .Max();

                string newLibraryName = $"{newLibraryNamePrefix}{currentMax + 1}";

                newLibrary = new(this, newLibraryName);
            }


            int loc = 0;
            while (loc < Libraries.Count && Libraries[loc].Name.CompareTo(newLibrary.Name) < 0) loc++;
            Libraries.Insert(loc, newLibrary);

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

        public string GetNextPrimaryKey(List<Category> failedCategories)
        {
            return Project.s_GetNextPrimaryKey(ProjectKeyPattern, Libraries.Select(l => l.GetNextPrimaryKey(failedCategories, ProjectKeyPattern)), false);
        }

        public void SaveToFile(string filePath)
        {
            File.WriteAllText(filePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true, ReferenceHandler = ReferenceHandler.Preserve }));
        }
    }
}
