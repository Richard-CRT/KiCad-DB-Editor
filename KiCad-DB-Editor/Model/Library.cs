using KiCad_DB_Editor.Commands;
using KiCad_DB_Editor.Model.Json;
using KiCad_DB_Editor.ViewModel;
using KiCad_DB_Editor.Utilities;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace KiCad_DB_Editor.Model
{
    public class Library : NotifyObject
    {
        public static Library FromScratch()
        {
            Library library = new();

            return library;
        }

        public static Library FromFile(string projectFilePath)
        {
            Library library = new();
            try
            {
                string? projectDirectory = Path.GetDirectoryName(projectFilePath);
                string? projectName = Path.GetFileNameWithoutExtension(projectFilePath);
                if (projectDirectory is null || projectDirectory == "" || projectName is null || projectName == "")
                    throw new InvalidOperationException();

                string componentsFilePath = Path.Combine(projectDirectory, projectName);
                componentsFilePath += ".sqlite3";

                // Must populate these before KiCadSymbolLibraries and KiCadFootprintLibraries
                library.ProjectDirectoryPath = projectDirectory;
                library.ProjectName = projectName;

                JsonLibrary jsonLibrary = JsonLibrary.FromFile(projectFilePath);

                library.PartUIDScheme = jsonLibrary.PartUIDScheme;
                library.KiCadExportPartLibraryName = jsonLibrary.KiCadExportPartLibraryName;
                library.KiCadExportPartLibraryDescription = jsonLibrary.KiCadExportPartLibraryDescription;
                library.KiCadExportOdbcName = jsonLibrary.KiCadExportOdbcName;
                library.KiCadAutoExportOnSave = jsonLibrary.KiCadAutoExportOnSave;
                library.KiCadAutoExportRelativePath = jsonLibrary.KiCadAutoExportRelativePath;
                library.AllParameters.AddRange(jsonLibrary.AllParameters.Select(jP => new Parameter(jP)));
                library.TopLevelCategories.AddRange(jsonLibrary.TopLevelCategories.Select(c => new Category(c, library, null)));
                library.AllCategories.AddRange(library.TopLevelCategories);
                for (int i = 0; i < library.AllCategories.Count; i++)
                    library.AllCategories.AddRange(library.AllCategories[i].Categories);
                library.KiCadSymbolLibraries.AddRange(jsonLibrary.KiCadSymbolLibraries.Select(kSL => new KiCadSymbolLibrary(kSL, library)));
                library.KiCadFootprintLibraries.AddRange(jsonLibrary.KiCadFootprintLibraries.Select(kFL => new KiCadFootprintLibrary(kFL, library)));

                List<string> dbPartColumnNames = new();
                List<Type> dbPartColumnTypes = new();
                List<List<object>> dbParts = new();
                using (var connection = new SqliteConnection($"Data Source={componentsFilePath}"))
                {
                    connection.Open();

                    // Worse DB structure but simpler for humans
                    string selectPartsSql = "SELECT * FROM \"Components\"";
                    var selectPartsCommand = connection.CreateCommand();
                    selectPartsCommand.CommandText = selectPartsSql;
                    using (var reader = selectPartsCommand.ExecuteReader())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            dbPartColumnNames.Add(reader.GetName(i));
                            dbPartColumnTypes.Add(reader.GetFieldType(i));
                        }
                        while (reader.Read())
                        {
                            List<object> part = new();
                            for (int i = 0; i < reader.FieldCount; i++)
                                part.Add(reader[i]);
                            dbParts.Add(part);
                        }
                    }
                }
                SqliteConnection.ClearAllPools();

                const int numberSpecialColumns = 12;
                int columnIndex = 0;
                if (
                    dbPartColumnNames.Count < numberSpecialColumns ||
                    dbPartColumnTypes[columnIndex] != typeof(string) ||
                    dbPartColumnNames[columnIndex++] != "Category" ||
                    dbPartColumnTypes[columnIndex] != typeof(string) ||
                    dbPartColumnNames[columnIndex++] != "Part UID" ||
                    dbPartColumnTypes[columnIndex] != typeof(string) ||
                    dbPartColumnNames[columnIndex++] != "Description" ||
                    dbPartColumnTypes[columnIndex] != typeof(string) ||
                    dbPartColumnNames[columnIndex++] != "Manufacturer" ||
                    dbPartColumnTypes[columnIndex] != typeof(string) ||
                    dbPartColumnNames[columnIndex++] != "MPN" ||
                    dbPartColumnTypes[columnIndex] != typeof(string) ||
                    dbPartColumnNames[columnIndex++] != "Value" ||
                    dbPartColumnTypes[columnIndex] != typeof(string) ||
                    dbPartColumnNames[columnIndex++] != "Datasheet" ||
                    dbPartColumnTypes[columnIndex] != typeof(Int64) ||
                    dbPartColumnNames[columnIndex++] != "Exclude from BOM" ||
                    dbPartColumnTypes[columnIndex] != typeof(Int64) ||
                    dbPartColumnNames[columnIndex++] != "Exclude from Board" ||
                    dbPartColumnTypes[columnIndex] != typeof(Int64) ||
                    dbPartColumnNames[columnIndex++] != "Exclude from Sim" ||
                    dbPartColumnTypes[columnIndex] != typeof(string) ||
                    dbPartColumnNames[columnIndex++] != "Symbol Library Name" ||
                    dbPartColumnTypes[columnIndex] != typeof(string) ||
                    dbPartColumnNames[columnIndex++] != "Symbol Name"
                    )
                    throw new InvalidDataException("Special columns not found or wrong type");

                int numberOfFootprintColumns = 0;
                for (int i = numberSpecialColumns; i < dbPartColumnNames.Count && dbPartColumnNames[i].StartsWith("Footprint"); i++, numberOfFootprintColumns++) ;
                if (numberOfFootprintColumns % 2 != 0)
                    throw new InvalidDataException("Footprint columns not an even number");

                // All the footprint columns need to be a string
                for (int i = numberSpecialColumns; i < numberSpecialColumns + numberOfFootprintColumns; i++)
                {
                    if (dbPartColumnTypes[i] != typeof(string))
                        throw new InvalidDataException($"Footprint columns {i} wrong type");
                }
                int maxFootprints = numberOfFootprintColumns / 2;

                // Everything after the footprints needs to be a string
                for (int i = numberSpecialColumns + numberOfFootprintColumns; i < dbPartColumnTypes.Count; i++)
                {
                    if (dbPartColumnTypes[i] != typeof(string))
                        throw new InvalidDataException($"Columns {i} wrong type");
                }

                Dictionary<int, int> footprintNumberToLibraryNameColumnIndexToMap = new();
                Dictionary<int, int> footprintNumberToNameColumnIndexToMap = new();

                // Already asserted it's greater than 0 and even
                for (int i = numberSpecialColumns; i < numberSpecialColumns + numberOfFootprintColumns; i += 2)
                {
                    // Footprint x Library Name
                    // Footprint x Name
                    var split1 = dbPartColumnNames[i].Split(' ');
                    var split2 = dbPartColumnNames[i + 1].Split(' ');
                    if (split1[0] != "Footprint" || split1[2] != "Library" || split1[3] != "Name" ||
                        split2[0] != "Footprint" || split1[3] != "Name" ||
                        split1[1] != split2[1])
                    {
                        throw new InvalidDataException("Footprint columns don't match expected format");
                    }
                    int footprintNumber = int.Parse(split1[1]);
                    footprintNumberToLibraryNameColumnIndexToMap[footprintNumber] = i;
                    footprintNumberToNameColumnIndexToMap[footprintNumber] = i + 1;
                }

                //Dictionary<int, Parameter> columnIndexToParameterMap = new();
                Dictionary<Parameter, int> parameterToColumnIndexToMap = new();
                for (int i = numberSpecialColumns + numberOfFootprintColumns; i < dbPartColumnNames.Count; i++)
                {
                    string columnName = dbPartColumnNames[i];

                    if (columnName.Length >= 36)
                    {
                        string potentialUUID = columnName[^36..];
                        if (Guid.TryParse(potentialUUID, out Guid guid))
                        {
                            string uuid = guid.ToString();
                            Parameter? matchingParameter = library.AllParameters.FirstOrDefault(p => p!.UUID == uuid, null);
                            if (matchingParameter is not null)
                            {
                                //columnIndexToParameterMap[i] = parameter;
                                parameterToColumnIndexToMap[matchingParameter] = i;
                            }
                            else
                                throw new InvalidDataException("Could not find a parameter to correspond to database column UUID");
                        }
                        else
                            throw new InvalidDataException("Could not parse UUID from column name");
                    }
                    else
                        throw new InvalidDataException("Column name not longer enough to find UUID substring");
                }

                foreach (List<object> dbPart in dbParts)
                {
                    string categoryString = (string)dbPart[0];
                    string[] categoryStringParts = categoryString.Split('/');
                    Category? workingCategory = null;
                    for (int i = 1; i < categoryStringParts.Length; i++)
                    {
                        IEnumerable<Category> collectionOfCategories;
                        if (workingCategory is null)
                            collectionOfCategories = library.TopLevelCategories;
                        else
                            collectionOfCategories = workingCategory.Categories;

                        bool match = false;
                        foreach (Category category in collectionOfCategories)
                        {
                            if (category.Name == categoryStringParts[i])
                            {
                                workingCategory = category;
                                match = true;
                                break;
                            }
                        }
                        if (!match)
                            throw new InvalidDataException("Could not match part category in database to library category");
                    }
                    if (workingCategory is null)
                        throw new InvalidDataException("Could not match part category in database to library category");
                    Category partCategory = workingCategory;

                    int j = 1;
                    string partUID = (string)dbPart[j++];
                    Part part = new(partUID, library, partCategory);
                    part.Description = (string)dbPart[j++];
                    part.Manufacturer = (string)dbPart[j++];
                    part.MPN = (string)dbPart[j++];
                    part.Value = (string)dbPart[j++];
                    part.Datasheet = (string)dbPart[j++];
                    part.ExcludeFromBOM = (Int64)dbPart[j++] == 1;
                    part.ExcludeFromBoard = (Int64)dbPart[j++] == 1;
                    part.ExcludeFromSim = (Int64)dbPart[j++] == 1;
                    part.SymbolLibraryName = (string)dbPart[j++];
                    part.SymbolName = (string)dbPart[j++];
                    for (int i = 1; i <= maxFootprints; i++)
                    {
                        object footprintLibraryNameValue = dbPart[footprintNumberToLibraryNameColumnIndexToMap[i]];
                        object footprintNameValue = dbPart[footprintNumberToNameColumnIndexToMap[i]];

                        if ((footprintLibraryNameValue is System.DBNull && footprintNameValue is not System.DBNull) ||
                            (footprintLibraryNameValue is not System.DBNull && footprintNameValue is System.DBNull))
                            throw new InvalidDataException("Part cannot have a footprint library but not a footprint name (or vice versa)");

                        // Already asserted that if one of them is not null they both aren't
                        if (footprintLibraryNameValue is not System.DBNull)
                        {
                            // Because we don't map it to an index on Part, if someone messes with the SQLite file this will squash
                            // the footprints down on the list so there's empty spaces (as we're using a list on Part, not a dict)
                            part.FootprintPairs.Add(((string)footprintLibraryNameValue, (string)footprintNameValue));
                        }
                    }
                    foreach (Parameter parameter in partCategory.InheritedAndNormalParameters)
                    {
                        object value = dbPart[parameterToColumnIndexToMap[parameter]];
                        // Default value to "" if parameter is expected to be present but is null
                        if (value is System.DBNull)
                            part.ParameterValues[parameter] = "";
                        else
                            part.ParameterValues[parameter] = (string)value;
                    }
                    library.AllParts.Add(part);
                    partCategory.Parts.Add(part);
                }
            }
            catch (FileNotFoundException)
            {
                throw;
            }

            return library;
        }

        // ======================================================================

        #region Notify Properties

        private string _partUIDScheme { get; set; } = "CMP-#######-####";
        public string PartUIDScheme
        {
            get { return _partUIDScheme; }
            set
            {
                if (this.PartUIDScheme != value)
                {
                    if (value.Count(c => c == '#') != Util.PartUIDSchemeNumberOfWildcards)
                        throw new Exceptions.ArgumentValidationException("Proposed scheme does not contain the necessary wildcard characters");

                    _partUIDScheme = value;
                    InvokePropertyChanged();
                }
            }
        }

        private string _kiCadExportPartLibraryName { get; set; } = "";
        public string KiCadExportPartLibraryName
        {
            get { return _kiCadExportPartLibraryName; }
            set { if (_kiCadExportPartLibraryName != value) { _kiCadExportPartLibraryName = value; InvokePropertyChanged(); } }
        }

        private string _kiCadExportPartLibraryDescription { get; set; } = "";
        public string KiCadExportPartLibraryDescription
        {
            get { return _kiCadExportPartLibraryDescription; }
            set { if (_kiCadExportPartLibraryDescription != value) { _kiCadExportPartLibraryDescription = value; InvokePropertyChanged(); } }
        }

        private string _kiCadExportOdbcName { get; set; } = "";
        public string KiCadExportOdbcName
        {
            get { return _kiCadExportOdbcName; }
            set { if (_kiCadExportOdbcName != value) { _kiCadExportOdbcName = value; InvokePropertyChanged(); } }
        }

        private bool _kiCadAutoExportOnSave { get; set; } = false;
        public bool KiCadAutoExportOnSave
        {
            get { return _kiCadAutoExportOnSave; }
            set { if (_kiCadAutoExportOnSave != value) { _kiCadAutoExportOnSave = value; InvokePropertyChanged(); } }
        }

        private string _kiCadAutoExportPath { get; set; } = "";
        public string KiCadAutoExportRelativePath
        {
            get { return _kiCadAutoExportPath; }
            set { if (_kiCadAutoExportPath != value) { _kiCadAutoExportPath = value; InvokePropertyChanged(); } }
        }

        // No setter, to prevent the VM needing to listening PropertyChanged events
        private ObservableCollectionEx<Part> _allParts;
        public ObservableCollectionEx<Part> AllParts
        {
            get { return _allParts; }
        }

        // No setter, to prevent the VM needing to listening PropertyChanged events
        private ObservableCollectionEx<Parameter> _allParameters;
        public ObservableCollectionEx<Parameter> AllParameters
        {
            get { return _allParameters; }
        }

        // No setter, to prevent the VM needing to listening PropertyChanged events
        private ObservableCollectionEx<Category> _allCategories;
        public ObservableCollectionEx<Category> AllCategories
        {
            get { return _allCategories; }
        }

        // No setter, to prevent the VM needing to listening PropertyChanged events
        private ObservableCollectionEx<Category> _topLevelCategories;
        public ObservableCollectionEx<Category> TopLevelCategories
        {
            get { return _topLevelCategories; }
        }

        // No setter, to prevent the VM needing to listening PropertyChanged events
        private ObservableCollectionEx<KiCadSymbolLibrary> _kiCadSymbolLibraries;
        public ObservableCollectionEx<KiCadSymbolLibrary> KiCadSymbolLibraries
        {
            get { return _kiCadSymbolLibraries; }
        }


        // No setter, to prevent the VM needing to listening PropertyChanged events
        private ObservableCollectionEx<KiCadFootprintLibrary> _kiCadFootprintLibraries;
        public ObservableCollectionEx<KiCadFootprintLibrary> KiCadFootprintLibraries
        {
            get { return _kiCadFootprintLibraries; }
        }

        #endregion Notify Properties

        public string ProjectDirectoryPath { get; set; } = "";

        public string ProjectName { get; set; } = "";

        public Library()
        {
            // Initialise collection with events
            _allParts = new();
            _allParameters = new();
            // We don't worry about unsubscribing because this object is the event publisher
            _allParameters.CollectionChanged += _allParameters_CollectionChanged;
            _allCategories = new();
            _topLevelCategories = new();
            _kiCadSymbolLibraries = new();
            _kiCadFootprintLibraries = new();
        }

        private void _allParameters_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var c in AllCategories) c.ParentLibrary_AllParameters_CollectionChanged(sender, e);
        }

        public bool WriteToFile(string projectFilePath, bool autosave = false)
        {
            // For this function we don't rely on anything being DB sanitised, even though we controlled the inputs
            // of some things. Hence lots of .Replace("'", "''") and .Replace("\"", "\"\"")
            try
            {
                projectFilePath = (new Uri(projectFilePath)).AbsolutePath;

                string? projectDirectory = Path.GetDirectoryName(projectFilePath);
                string? projectName = Path.GetFileNameWithoutExtension(projectFilePath);
                string? fileExtension = Path.GetExtension(projectFilePath);

                if (projectDirectory is null || projectDirectory == "" || !Directory.Exists(projectDirectory) || projectName is null || projectName == "" || fileExtension is null || fileExtension != ".kidbe_proj")
                    throw new InvalidOperationException();

                this.ProjectDirectoryPath = projectDirectory;
                this.ProjectName = projectName;

                string componentsFilePath = Path.Combine(this.ProjectDirectoryPath, this.ProjectName);
                componentsFilePath += ".sqlite3";

                if (autosave)
                {
                    projectFilePath += ".autosave";
                    componentsFilePath += ".autosave";
                }

                string tempProjectPath = $"proj.tmp";
                string tempComponentsPath = $"components.tmp";

                // Have to create a JsonLibrary to serialise it

                JsonLibrary jsonLibrary = new JsonLibrary(this);
                if (!jsonLibrary.WriteToFile(tempProjectPath, autosave)) return false;

                Dictionary<Category, string> categoryToCategoryStringMap = new();
                foreach (Category category in AllCategories)
                {
                    string path = $"/{category.Name}";
                    var c = category;
                    while (c.ParentCategory is not null)
                    {
                        path = $"/{c.ParentCategory.Name}{path}";
                        c = c.ParentCategory;
                    }
                    categoryToCategoryStringMap[category] = path;
                }

                int maxFootprints = 0;
                foreach (Part part in AllParts)
                    maxFootprints = Math.Max(maxFootprints, part.FootprintPairs.Count);

                File.Delete(tempComponentsPath);
                using (var connection = new SqliteConnection($"Data Source={tempComponentsPath}"))
                {
                    connection.Open();

                    // Worse DB structure but simpler for humans
                    string createTableSql = "CREATE TABLE \"Components\" (" +
                        "\"Category\" TEXT, " +
                        "\"Part UID\" TEXT, " +
                        "\"Description\" TEXT, " +
                        "\"Manufacturer\" TEXT, " +
                        "\"MPN\" TEXT, " +
                        "\"Value\" TEXT, " +
                        "\"Datasheet\" TEXT, " +
                        "\"Exclude from BOM\" INTEGER, " +
                        "\"Exclude from Board\" INTEGER, " +
                        "\"Exclude from Sim\" INTEGER, " +
                        "\"Symbol Library Name\" TEXT, " +
                        "\"Symbol Name\" TEXT, "
                        ;
                    for (int j = 1; j <= maxFootprints; j++)
                    {
                        createTableSql += $"\"Footprint {j} Library Name\" TEXT, ";
                        createTableSql += $"\"Footprint {j} Name\" TEXT, ";
                    }
                    foreach (Parameter parameter in AllParameters)
                        createTableSql += $"\"{parameter.Name.Replace("\"", "\"\"")} {parameter.UUID}\" TEXT, ";
                    createTableSql = createTableSql[..^2];
                    createTableSql += ")";

                    var createTableCommand = connection.CreateCommand();
                    createTableCommand.CommandText = createTableSql;

                    createTableCommand.ExecuteNonQuery();

                    if (AllParts.Count > 0)
                    {
                        string insertPartsSql = "INSERT INTO \"Components\" (" +
                            "\"Category\", " +
                            "\"Part UID\", " +
                            "\"Description\", " +
                            "\"Manufacturer\", " +
                            "\"MPN\", " +
                            "\"Value\", " +
                            "\"Datasheet\", " +
                            "\"Exclude from BOM\", " +
                            "\"Exclude from Board\", " +
                            "\"Exclude from Sim\", " +
                            "\"Symbol Library Name\", " +
                            "\"Symbol Name\", ";
                        for (int j = 1; j <= maxFootprints; j++)
                        {
                            insertPartsSql += $"\"Footprint {j} Library Name\", ";
                            insertPartsSql += $"\"Footprint {j} Name\", ";
                        }
                        foreach (Parameter parameter in AllParameters)
                            insertPartsSql += $"\"{parameter.Name.Replace("\"", "\"\"")} {parameter.UUID}\", ";
                        insertPartsSql = insertPartsSql[..^2];
                        insertPartsSql += ") VALUES ";
                        foreach (Part part in AllParts)
                        {
                            insertPartsSql += "(" +
                                    $"'{categoryToCategoryStringMap[part.ParentCategory]}', " +
                                    $"'{part.PartUID.Replace("'", "''")}', " +
                                    $"'{part.Description.Replace("'", "''")}', " +
                                    $"'{part.Manufacturer.Replace("'", "''")}', " +
                                    $"'{part.MPN.Replace("'", "''")}', " +
                                    $"'{part.Value.Replace("'", "''")}', " +
                                    $"'{part.Datasheet.Replace("'", "''")}', " +
                                    $"{(part.ExcludeFromBOM ? 1 : 0)}, " +
                                    $"{(part.ExcludeFromBoard ? 1 : 0)}, " +
                                    $"{(part.ExcludeFromSim ? 1 : 0)}, " +
                                    $"'{part.SymbolLibraryName.Replace("'", "''")}', " +
                                    $"'{part.SymbolName.Replace("'", "''")}', ";
                            for (int j = 1; j <= maxFootprints; j++)
                            {
                                // We've previously asserted part.FootprintLibraryNames.Count == part.FootprintNames.Count
                                if (j <= part.FootprintPairs.Count)
                                {
                                    insertPartsSql += $"'{part.FootprintPairs[j - 1].Item1.Replace("'", "''")}', ";
                                    insertPartsSql += $"'{part.FootprintPairs[j - 1].Item2.Replace("'", "''")}', ";
                                }
                                else
                                {
                                    insertPartsSql += $"NULL, NULL, ";
                                }
                            }
                            foreach (Parameter parameter in AllParameters)
                            {
                                if (part.ParameterValues.TryGetValue(parameter, out string? value))
                                    insertPartsSql += $"'{value.Replace("'", "''")}', ";
                                else
                                    insertPartsSql += $"NULL, ";
                            }
                            insertPartsSql = insertPartsSql[..^2];
                            insertPartsSql += "), ";
                        }
                        insertPartsSql = insertPartsSql[..^2];

                        var insertPartsCommand = connection.CreateCommand();
                        insertPartsCommand.CommandText = insertPartsSql;

                        insertPartsCommand.ExecuteNonQuery();
                    }

                }
                SqliteConnection.ClearAllPools();

                File.Move(tempProjectPath, projectFilePath, overwrite: true);
                File.Move(tempComponentsPath, componentsFilePath, overwrite: true);

                return true;
            }
            catch (Exception)
            {
                SqliteConnection.ClearAllPools();
                return false;
            }
        }

        public bool ExportToKiCad(bool autoExport, string kiCadDbConfFilePath = "")
        {
            // For this function we don't rely on anything being DB sanitised, even though we controlled the inputs
            // of some things. Hence lots of .Replace("'", "''") and .Replace("\"", "\"\"")
            try
            {
                if (autoExport)
                    kiCadDbConfFilePath = (new Uri(Path.Combine(ProjectDirectoryPath, KiCadAutoExportRelativePath))).AbsolutePath;

                string? parentDirectory = Path.GetDirectoryName(kiCadDbConfFilePath);
                string? fileName = Path.GetFileNameWithoutExtension(kiCadDbConfFilePath);
                string? fileExtension = Path.GetExtension(kiCadDbConfFilePath);

                if (parentDirectory is null || parentDirectory == "" || !Directory.Exists(parentDirectory) || fileName is null || fileName == "" || fileExtension is null || fileExtension != ".kicad_dbl")
                    throw new InvalidOperationException();

                string kiCadSqliteFilePath = Path.Combine(parentDirectory, fileName);
                kiCadSqliteFilePath += ".sqlite3";

                string tempDbConfPath = $"db_conf.tmp";
                string tempSqlitePath = $"sqlite.tmp";

                Dictionary<Category, string> categoryToKiCadExportCategoryStringMap = new();
                foreach (Category category in AllCategories)
                {
                    string path = $"{category.Name}";
                    var c = category;
                    while (c.ParentCategory is not null)
                    {
                        path = $"{c.ParentCategory.Name} | {path}";
                        c = c.ParentCategory;
                    }
                    categoryToKiCadExportCategoryStringMap[category] = path;
                }

                JsonKiCadDblFile jsonKiCadDblFile = new(this.KiCadExportPartLibraryName, this.KiCadExportPartLibraryDescription, this.KiCadExportOdbcName);

                File.Delete(tempSqlitePath);
                using (var connection = new SqliteConnection($"Data Source={tempSqlitePath}"))
                {
                    connection.Open();

                    foreach (Category category in AllCategories)
                    {

                        string tableName = categoryToKiCadExportCategoryStringMap[category];

                        JsonKiCadDbl_Library jsonKiCadDbl_Library = new(tableName, tableName, "Part UID", "Schematic Symbol", "Footprints");
                        jsonKiCadDblFile.jsonKiCadDbl_Libraries.Add(jsonKiCadDbl_Library);

                        // Fields
                        jsonKiCadDbl_Library.jsonKiCadDbl_Library_Fields.Add(new("Part UID", "Part UID", true, true));
                        jsonKiCadDbl_Library.jsonKiCadDbl_Library_Fields.Add(new("Manufacturer", "Manufacturer", true, true));
                        jsonKiCadDbl_Library.jsonKiCadDbl_Library_Fields.Add(new("MPN", "MPN", true, true));
                        jsonKiCadDbl_Library.jsonKiCadDbl_Library_Fields.Add(new("Value", "Value", true, true));
                        jsonKiCadDbl_Library.jsonKiCadDbl_Library_Fields.Add(new("Datasheet", "Datasheet", true, true));

                        // Properties
                        // Not sure why description is here instead of in fields but it's KiCad's rule
                        jsonKiCadDbl_Library.jsonKiCadDbl_Library_Properties.Add("description", "Description");
                        jsonKiCadDbl_Library.jsonKiCadDbl_Library_Properties.Add("exclude_from_bom", "Exclude from BOM");
                        jsonKiCadDbl_Library.jsonKiCadDbl_Library_Properties.Add("exclude_from_board", "Exclude from Board");
                        jsonKiCadDbl_Library.jsonKiCadDbl_Library_Properties.Add("exclude_from_sim", "Exclude from Sim");

                        string createTableSql = $"CREATE TABLE \"{tableName.Replace("\"", "\"\"")}\" (" +
                            "\"Part UID\" TEXT, " +
                            "\"Description\" TEXT, " +
                            "\"Manufacturer\" TEXT, " +
                            "\"MPN\" TEXT, " +
                            "\"Value\" TEXT, " +
                            "\"Datasheet\" TEXT, ";

                        // Should already be in order as we reshuffle the lists accordingly, but may as well do the .Intersect just to confirm it
                        var categoryParametersInOrder = AllParameters.Intersect(category.InheritedAndNormalParameters);
                        foreach (var parameter in categoryParametersInOrder)
                        {
                            jsonKiCadDbl_Library.jsonKiCadDbl_Library_Fields.Add(new(parameter.Name, parameter.Name, true, true));
                            createTableSql += $"\"{parameter.Name.Replace("\"", "\"\"")}\" TEXT, ";
                        }

                        createTableSql +=
                            "\"Schematic Symbol\" TEXT, " +
                            "\"Footprints\" TEXT, " +
                            "\"Exclude from BOM\" TEXT, " +
                            "\"Exclude from Board\" TEXT, " +
                            "\"Exclude from Sim\" TEXT)";

                        var createTableCommand = connection.CreateCommand();
                        createTableCommand.CommandText = createTableSql;

                        createTableCommand.ExecuteNonQuery();

                        if (category.Parts.Count > 0)
                        {
                            string insertPartsSql = $"INSERT INTO \"{categoryToKiCadExportCategoryStringMap[category].Replace("\"", "\"\"")}\" (" +
                                "\"Part UID\", " +
                                "\"Description\", " +
                                "\"Manufacturer\", " +
                                "\"MPN\", " +
                                "\"Value\", " +
                                "\"Datasheet\", ";

                            foreach (var parameter in categoryParametersInOrder)
                                insertPartsSql += $"\"{parameter.Name.Replace("\"", "\"\"")}\", ";

                            insertPartsSql +=
                                "\"Schematic Symbol\", " +
                                "\"Footprints\", " +
                                "\"Exclude from BOM\", " +
                                "\"Exclude from Board\", " +
                                "\"Exclude from Sim\") VALUES ";
                            foreach (Part part in category.Parts)
                            {
                                string footprintsString = "";
                                for (int i = 0; i < part.FootprintPairs.Count; i++)
                                {
                                    footprintsString += $"{part.FootprintPairs[i].Item1}:{part.FootprintPairs[i].Item2}";
                                    if (i < part.FootprintPairs.Count - 1)
                                        footprintsString += ";";
                                }

                                insertPartsSql +=
                                    "(" +
                                    $"'{part.PartUID.Replace("'", "''")}', " +
                                    $"'{part.Description.Replace("'", "''")}', " +
                                    $"'{part.Manufacturer.Replace("'", "''")}', " +
                                    $"'{part.MPN.Replace("'", "''")}', " +
                                    $"'{part.Value.Replace("'", "''")}', " +
                                    $"'{part.Datasheet.Replace("'", "''")}', ";

                                foreach (var parameter in categoryParametersInOrder)
                                    insertPartsSql += $"\"{part.ParameterValues[parameter].Replace("\"", "\"\"")}\", ";

                                insertPartsSql +=
                                    $"'{part.SymbolLibraryName.Replace("'", "''")}:{part.SymbolName.Replace("'", "''")}', " +
                                    $"'{footprintsString.Replace("'", "''")}', " +
                                    $"{(part.ExcludeFromBOM ? 1 : 0)}, " +
                                    $"{(part.ExcludeFromBoard ? 1 : 0)}, " +
                                    $"{(part.ExcludeFromSim ? 1 : 0)}), ";
                            }
                            insertPartsSql = insertPartsSql[..^2];

                            var insertPartsCommand = connection.CreateCommand();
                            insertPartsCommand.CommandText = insertPartsSql;

                            insertPartsCommand.ExecuteNonQuery();
                        }

                    }
                }
                SqliteConnection.ClearAllPools();

                if (!jsonKiCadDblFile.WriteToFile(tempDbConfPath)) return false;

                // SqliteConnection hasn't properly closed the file at this point, force a GC to ensure it's closed
                GC.Collect();
                GC.WaitForPendingFinalizers();

                File.Move(tempDbConfPath, kiCadDbConfFilePath, overwrite: true);
                File.Move(tempSqlitePath, kiCadSqliteFilePath, overwrite: true);

                return true;
            }
            catch (Exception)
            {
                SqliteConnection.ClearAllPools();
                return false;
            }
        }
    }
}
