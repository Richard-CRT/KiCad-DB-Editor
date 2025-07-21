using KiCad_DB_Editor.Commands;
using KiCad_DB_Editor.Model.Json;
using KiCad_DB_Editor.Utilities;
using KiCad_DB_Editor.ViewModel;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
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

                string dataFilePath = Path.Combine(projectDirectory, projectName);
                dataFilePath += ".sqlite3";

                // Must populate these before KiCadSymbolLibraries and KiCadFootprintLibraries
                library.ProjectDirectoryPath = projectDirectory;
                library.ProjectName = projectName;

                JsonLibrary jsonLibrary = JsonLibrary.FromFile(projectFilePath);

                library.PartUIDScheme = jsonLibrary.PartUIDScheme;
                library.KiCadExportPartLibraryName = jsonLibrary.KiCadExportPartLibraryName;
                library.KiCadExportPartLibraryDescription = jsonLibrary.KiCadExportPartLibraryDescription;
                library.KiCadExportPartLibraryEnvironmentVariable = jsonLibrary.KiCadExportPartLibraryEnvironmentVariable;
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

                Dictionary<string, Category> categoryStringToCategoryMap = new();
                foreach (Category category in library.AllCategories)
                {
                    string path = $"/{category.Name}";
                    var c = category;
                    while (c.ParentCategory is not null)
                    {
                        path = $"/{c.ParentCategory.Name}{path}";
                        c = c.ParentCategory;
                    }
                    categoryStringToCategoryMap[path] = category;
                }
                Dictionary<string, Parameter> parameterUuidToParameterMap = new();
                foreach (Parameter parameter in library.AllParameters)
                    parameterUuidToParameterMap[parameter.UUID] = parameter;

                using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
                {
                    connection.Open();

                    Dictionary<Int64, Category> categoryIdToCategory = new();
                    Dictionary<Int64, Parameter> parameterIdToParameter = new();
                    Dictionary<Int64, Part> partIdToPart = new();

                    var selectCategoriesCommand = connection.CreateCommand();
                    selectCategoriesCommand.CommandText = "SELECT * FROM \"Categories\"";
                    using (var reader = selectCategoriesCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var categoryId = (Int64)reader["ID"];
                            var categoryString = (string)reader["String"];
                            categoryIdToCategory[categoryId] = categoryStringToCategoryMap[categoryString];
                        }
                    }

                    var selectParametersCommand = connection.CreateCommand();
                    selectParametersCommand.CommandText = "SELECT * FROM \"Parameters\"";
                    using (var reader = selectParametersCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var parameterId = (Int64)reader["ID"];
                            var parameterUuid = (string)reader["UUID"];
                            parameterIdToParameter[parameterId] = parameterUuidToParameterMap[parameterUuid];
                        }
                    }

                    var selectPartsCommand = connection.CreateCommand();
                    selectPartsCommand.CommandText = "SELECT * FROM \"Parts\"";
                    using (var reader = selectPartsCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var partId = (Int64)reader["ID"];
                            var categoryId = (Int64)reader["Category ID"];
                            var partUID = (string)reader["Part UID"];
                            var description = (string)reader["Description"];
                            var manufacturer = (string)reader["Manufacturer"];
                            var mpn = (string)reader["MPN"];
                            var value = (string)reader["Value"];
                            var datasheet = (string)reader["Datasheet"];
                            var excludeFromBOM = (Int64)reader["Exclude from BOM"];
                            var excludeFromBoard = (Int64)reader["Exclude from Board"];
                            var excludeFromSim = (Int64)reader["Exclude from Sim"];
                            var symbolLibraryName = (string)reader["Symbol Library Name"];
                            var symbolName = (string)reader["Symbol Name"];

                            Category category = categoryIdToCategory[categoryId];

                            Part part = new(partUID, library, category);
                            part.Description = description;
                            part.Manufacturer = manufacturer;
                            part.MPN = mpn;
                            part.Value = value;
                            part.Datasheet = datasheet;
                            part.ExcludeFromBOM = excludeFromBOM == 1;
                            part.ExcludeFromBoard = excludeFromBoard == 1;
                            part.ExcludeFromSim = excludeFromSim == 1;
                            part.SymbolLibraryName = symbolLibraryName;
                            part.SymbolName = symbolName;

                            partIdToPart[partId] = part;
                            library.AllParts.Add(part);
                            category.Parts.Add(part);
                        }
                    }

                    var selectPartParameterLinksCommand = connection.CreateCommand();
                    selectPartsCommand.CommandText = "SELECT * FROM \"PartParameterLinks\"";
                    using (var reader = selectPartsCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var partId = (Int64)reader["Part ID"];
                            var parameterId = (Int64)reader["Parameter ID"];
                            var value = (string)reader["Value"];

                            var part = partIdToPart[partId];
                            var parameter = parameterIdToParameter[parameterId];
                            part.ParameterValues[parameter] = value;
                        }
                    }

                    var selectPartFootprintsCommand = connection.CreateCommand();
                    // Needs to be order by ID ASC as this determines which number the footprint is on the part
                    selectPartFootprintsCommand.CommandText = "SELECT * FROM \"PartFootprints\" ORDER BY \"ID\" ASC";
                    using (var reader = selectPartFootprintsCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var partId = (Int64)reader["Part ID"];
                            var libraryName = (string)reader["Library Name"];
                            var name = (string)reader["Name"];

                            var part = partIdToPart[partId];
                            part.FootprintPairs.Add((libraryName, name));
                        }
                    }
                }
                SqliteConnection.ClearAllPools();
            }
            catch (FileNotFoundException)
            {
                throw;
            }

            return library;
        }

        // ======================================================================

        #region Notify Properties

        private string _projectDirectoryPath = "";
        public string ProjectDirectoryPath
        {
            get { return _projectDirectoryPath; }
            set { if (_projectDirectoryPath != value) { _projectDirectoryPath = value; InvokePropertyChanged(); } }
        }

        private string _projectName = "";
        public string ProjectName
        {
            get { return _projectName; }
            set { if (_projectName != value) { _projectName = value; InvokePropertyChanged(); } }
        }

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

        private string _kiCadExportPartLibraryEnvironmentVariable { get; set; } = "${KICAD_PART_LIBRARY_PROJECT_DIR}";
        public string KiCadExportPartLibraryEnvironmentVariable
        {
            get { return _kiCadExportPartLibraryEnvironmentVariable; }
            set { if (_kiCadExportPartLibraryEnvironmentVariable != value) { _kiCadExportPartLibraryEnvironmentVariable = value; InvokePropertyChanged(); } }
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

        public ObservableCollectionEx<string> AllManufacturers
        {
            get { return new ObservableCollectionEx<string>(AllParts.Select(p => p.Manufacturer).ToHashSet().Where(m => !string.IsNullOrEmpty(m)).Order()); }
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

        private Parameter[]? oldAllParameters = null;
        private void _allParameters_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (oldAllParameters is not null)
                foreach (var parameter in oldAllParameters)
                    parameter.PropertyChanged -= AllParameterChanged;
            oldAllParameters = _allParameters.ToArray();
            foreach (var parameter in _allParameters)
                parameter.PropertyChanged += AllParameterChanged;

            foreach (var c in AllCategories) c.ParentLibrary_AllParameters_CollectionChanged(sender, e);
        }

        private void AllParameterChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            foreach (var c in AllCategories) c.ParentLibrary_AllParameter_PropertyChanged(sender, e);
        }

        public bool WriteToFile(string projectFilePath, bool autosave = false)
        {
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

                string dataFilePath = Path.Combine(this.ProjectDirectoryPath, this.ProjectName);
                dataFilePath += ".sqlite3";

                if (autosave)
                {
                    projectFilePath += ".autosave";
                    dataFilePath += ".autosave";
                }

                string tempProjectPath = $"proj.tmp";
                string tempDataPath = $"data.tmp";

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

                File.Delete(tempDataPath);
                using (var connection = new SqliteConnection($"Data Source={tempDataPath}"))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        Dictionary<Category, Int64> categoryToCategoryId = new();
                        Dictionary<Parameter, Int64> parameterToParameterId = new();

                        var createTablesCommand = connection.CreateCommand();
                        createTablesCommand.CommandText = @"
CREATE TABLE ""Categories"" (
    ""ID"" INTEGER,
    ""String"" TEXT,
    PRIMARY KEY(""ID"" AUTOINCREMENT)
);
CREATE TABLE ""Parameters"" (
    ""ID"" INTEGER,
    ""UUID"" TEXT,
    ""Name"" TEXT,
    PRIMARY KEY(""ID"" AUTOINCREMENT)
);
CREATE TABLE ""Parts"" (
    ""ID"" INTEGER,
    ""Category ID"" INTEGER,
    ""Part UID"" TEXT,
    ""Description"" TEXT,
    ""Manufacturer"" TEXT,
    ""MPN"" TEXT,
    ""Value"" TEXT,
    ""Datasheet"" TEXT,
    ""Exclude from BOM"" INTEGER,
    ""Exclude from Board"" INTEGER,
    ""Exclude from Sim"" INTEGER,
    ""Symbol Library Name"" TEXT,
    ""Symbol Name"" TEXT,
    PRIMARY KEY(""ID"" AUTOINCREMENT)
);
CREATE TABLE ""PartParameterLinks"" (
    ""Part ID"" INTEGER,
    ""Parameter ID"" INTEGER,
    ""Value"" TEXT,
    PRIMARY KEY(""Part ID"", ""Parameter ID"")
);
CREATE TABLE ""PartFootprints"" (
    ""ID"" INTEGER,
    ""Part ID"" INTEGER,
    ""Library Name"" INTEGER,
    ""Name"" TEXT,
    PRIMARY KEY(""ID"" AUTOINCREMENT)
);
";
                        createTablesCommand.ExecuteNonQuery();

                        var insertCategoryCommand = connection.CreateCommand();
                        insertCategoryCommand.CommandText = @"
INSERT INTO ""Categories"" (""String"")
VALUES (
    $category_string
)
RETURNING ""ID""
";

                        // Doesn't actually seem to affect performance, but adding for completeness
                        insertCategoryCommand.Prepare();

                        var insertCategoryCommand_CategoryStringParameter = insertCategoryCommand.CreateParameter();
                        insertCategoryCommand_CategoryStringParameter.ParameterName = "$category_string";
                        insertCategoryCommand.Parameters.Add(insertCategoryCommand_CategoryStringParameter);

                        foreach (var category in AllCategories)
                        {
                            insertCategoryCommand_CategoryStringParameter.Value = categoryToCategoryStringMap[category];
                            var id = (Int64)insertCategoryCommand.ExecuteScalar()!;
                            categoryToCategoryId[category] = id;
                        }

                        var insertParameterCommand = connection.CreateCommand();
                        insertParameterCommand.CommandText = @"
INSERT INTO ""Parameters"" (""UUID"", ""Name"")
VALUES (
    $uuid,
    $name
)
RETURNING ""ID""
";

                        var insertParameterCommand_UuidParameter = insertParameterCommand.CreateParameter();
                        insertParameterCommand_UuidParameter.ParameterName = "$uuid";
                        insertParameterCommand.Parameters.Add(insertParameterCommand_UuidParameter);

                        var insertParameterCommand_NameParameter = insertParameterCommand.CreateParameter();
                        insertParameterCommand_NameParameter.ParameterName = "name";
                        insertParameterCommand.Parameters.Add(insertParameterCommand_NameParameter);

                        // Doesn't actually seem to affect performance, but adding for completeness
                        insertParameterCommand.Prepare();

                        foreach (var parameter in AllParameters)
                        {
                            insertParameterCommand_UuidParameter.Value = parameter.UUID;
                            insertParameterCommand_NameParameter.Value = parameter.Name;

                            var id = (Int64)insertParameterCommand.ExecuteScalar()!;
                            parameterToParameterId[parameter] = id;
                        }

                        var insertPartCommand = connection.CreateCommand();
                        insertPartCommand.CommandText = @"
INSERT INTO ""Parts"" (
    ""Category ID"",
    ""Part UID"",
    ""Description"",
    ""Manufacturer"",
    ""MPN"",
    ""Value"",
    ""Datasheet"",
    ""Exclude from BOM"",
    ""Exclude from Board"",
    ""Exclude from Sim"",
    ""Symbol Library Name"",
    ""Symbol Name""
)
VALUES (
    $category_id,
    $part_uid,
    $description,
    $manufacturer,
    $mpn,
    $value,
    $datasheet,
    $exclude_from_bom,
    $exclude_from_board,
    $exclude_from_sim,
    $symbol_lib_name,
    $symbol_name
)
RETURNING ""ID""
";
                        var insertPartCommand_CategoryIdParameter = insertPartCommand.CreateParameter();
                        insertPartCommand_CategoryIdParameter.ParameterName = "$category_id";
                        insertPartCommand.Parameters.Add(insertPartCommand_CategoryIdParameter);

                        var insertPartCommand_PartUIDParameter = insertPartCommand.CreateParameter();
                        insertPartCommand_PartUIDParameter.ParameterName = "$part_uid";
                        insertPartCommand.Parameters.Add(insertPartCommand_PartUIDParameter);

                        var insertPartCommand_DescriptionParameter = insertPartCommand.CreateParameter();
                        insertPartCommand_DescriptionParameter.ParameterName = "$description";
                        insertPartCommand.Parameters.Add(insertPartCommand_DescriptionParameter);

                        var insertPartCommand_ManufacturerParameter = insertPartCommand.CreateParameter();
                        insertPartCommand_ManufacturerParameter.ParameterName = "$manufacturer";
                        insertPartCommand.Parameters.Add(insertPartCommand_ManufacturerParameter);

                        var insertPartCommand_MpnParameter = insertPartCommand.CreateParameter();
                        insertPartCommand_MpnParameter.ParameterName = "$mpn";
                        insertPartCommand.Parameters.Add(insertPartCommand_MpnParameter);

                        var insertPartCommand_ValueParameter = insertPartCommand.CreateParameter();
                        insertPartCommand_ValueParameter.ParameterName = "$value";
                        insertPartCommand.Parameters.Add(insertPartCommand_ValueParameter);

                        var insertPartCommand_DatasheetParameter = insertPartCommand.CreateParameter();
                        insertPartCommand_DatasheetParameter.ParameterName = "$datasheet";
                        insertPartCommand.Parameters.Add(insertPartCommand_DatasheetParameter);

                        var insertPartCommand_ExcludeFromBomParameter = insertPartCommand.CreateParameter();
                        insertPartCommand_ExcludeFromBomParameter.ParameterName = "$exclude_from_bom";
                        insertPartCommand.Parameters.Add(insertPartCommand_ExcludeFromBomParameter);

                        var insertPartCommand_ExcludeFromBoardParameter = insertPartCommand.CreateParameter();
                        insertPartCommand_ExcludeFromBoardParameter.ParameterName = "$exclude_from_board";
                        insertPartCommand.Parameters.Add(insertPartCommand_ExcludeFromBoardParameter);

                        var insertPartCommand_ExcludeFromSimParameter = insertPartCommand.CreateParameter();
                        insertPartCommand_ExcludeFromSimParameter.ParameterName = "$exclude_from_sim";
                        insertPartCommand.Parameters.Add(insertPartCommand_ExcludeFromSimParameter);

                        var insertPartCommand_SymbolLibNameParameter = insertPartCommand.CreateParameter();
                        insertPartCommand_SymbolLibNameParameter.ParameterName = "$symbol_lib_name";
                        insertPartCommand.Parameters.Add(insertPartCommand_SymbolLibNameParameter);

                        var insertPartCommand_SymbolNameParameter = insertPartCommand.CreateParameter();
                        insertPartCommand_SymbolNameParameter.ParameterName = "$symbol_name";
                        insertPartCommand.Parameters.Add(insertPartCommand_SymbolNameParameter);

                        // Doesn't actually seem to affect performance, but adding for completeness
                        insertPartCommand.Prepare();


                        var insertPartParameterLinkCommand = connection.CreateCommand();
                        insertPartParameterLinkCommand.CommandText = @"
INSERT INTO ""PartParameterLinks"" (""Part ID"", ""Parameter ID"", ""Value"")
VALUES (
    $part_id,
    $parameter_id,
    $value
)
";
                        var insertPartParameterLinkCommand_PartIdParameter = insertPartParameterLinkCommand.CreateParameter();
                        insertPartParameterLinkCommand_PartIdParameter.ParameterName = "part_id";
                        insertPartParameterLinkCommand.Parameters.Add(insertPartParameterLinkCommand_PartIdParameter);

                        var insertPartParameterLinkCommand_ParameterIdParameter = insertPartParameterLinkCommand.CreateParameter();
                        insertPartParameterLinkCommand_ParameterIdParameter.ParameterName = "parameter_id";
                        insertPartParameterLinkCommand.Parameters.Add(insertPartParameterLinkCommand_ParameterIdParameter);

                        var insertPartParameterLinkCommand_ValueParameter = insertPartParameterLinkCommand.CreateParameter();
                        insertPartParameterLinkCommand_ValueParameter.ParameterName = "value";
                        insertPartParameterLinkCommand.Parameters.Add(insertPartParameterLinkCommand_ValueParameter);

                        // Doesn't actually seem to affect performance, but adding for completeness
                        insertPartParameterLinkCommand.Prepare();


                        var insertPartFootprintCommand = connection.CreateCommand();
                        insertPartFootprintCommand.CommandText = @"
INSERT INTO ""PartFootprints"" (""Part ID"", ""Library Name"", ""Name"")
VALUES (
    $part_id,
    $library_name,
    $name
)
";
                        var insertPartFootprintCommand_PartIdParameter = insertPartFootprintCommand.CreateParameter();
                        insertPartFootprintCommand_PartIdParameter.ParameterName = "part_id";
                        insertPartFootprintCommand.Parameters.Add(insertPartFootprintCommand_PartIdParameter);

                        var insertPartFootprintCommand_LibraryNameParameter = insertPartFootprintCommand.CreateParameter();
                        insertPartFootprintCommand_LibraryNameParameter.ParameterName = "library_name";
                        insertPartFootprintCommand.Parameters.Add(insertPartFootprintCommand_LibraryNameParameter);

                        var insertPartFootprintCommand_NameParameter = insertPartFootprintCommand.CreateParameter();
                        insertPartFootprintCommand_NameParameter.ParameterName = "name";
                        insertPartFootprintCommand.Parameters.Add(insertPartFootprintCommand_NameParameter);

                        // Doesn't actually seem to affect performance, but adding for completeness
                        insertPartFootprintCommand.Prepare();

                        foreach (Part part in AllParts.OrderBy(p => p.PartUID))
                        {
                            insertPartCommand_CategoryIdParameter.Value = categoryToCategoryId[part.ParentCategory];
                            insertPartCommand_PartUIDParameter.Value = part.PartUID;
                            insertPartCommand_DescriptionParameter.Value = part.Description;
                            insertPartCommand_ManufacturerParameter.Value = part.Manufacturer;
                            insertPartCommand_MpnParameter.Value = part.MPN;
                            insertPartCommand_ValueParameter.Value = part.Value;
                            insertPartCommand_DatasheetParameter.Value = part.Datasheet;
                            insertPartCommand_ExcludeFromBomParameter.Value = part.ExcludeFromBOM;
                            insertPartCommand_ExcludeFromBoardParameter.Value = part.ExcludeFromBoard;
                            insertPartCommand_ExcludeFromSimParameter.Value = part.ExcludeFromSim;
                            insertPartCommand_SymbolLibNameParameter.Value = part.SymbolLibraryName;
                            insertPartCommand_SymbolNameParameter.Value = part.SymbolName;

                            var partId = (Int64)insertPartCommand.ExecuteScalar()!;

                            insertPartParameterLinkCommand_PartIdParameter.Value = partId;
                            insertPartFootprintCommand_PartIdParameter.Value = partId;

                            foreach ((Parameter parameter, string value) in part.ParameterValues)
                            {
                                Int64 parameterId = parameterToParameterId[parameter];
                                insertPartParameterLinkCommand_ParameterIdParameter.Value = parameterId;
                                insertPartParameterLinkCommand_ValueParameter.Value = value;

                                insertPartParameterLinkCommand.ExecuteNonQuery();
                            }
                            foreach ((string footprintLibraryName, string footprintName) in part.FootprintPairs)
                            {
                                insertPartFootprintCommand_LibraryNameParameter.Value = footprintLibraryName;
                                insertPartFootprintCommand_NameParameter.Value = footprintName;

                                insertPartFootprintCommand.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                }
                SqliteConnection.ClearAllPools();

                File.Copy(tempProjectPath, projectFilePath, overwrite: true);
                File.Copy(tempDataPath, dataFilePath, overwrite: true);

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
                    using (var transaction = connection.BeginTransaction())
                    {
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

                            var createTableCommand = connection.CreateCommand();
                            StringBuilder createTableSqlStringBuilder = new();
                            createTableSqlStringBuilder.Append($@"
CREATE TABLE ""{tableName.Replace("\"", "\"\"")}"" (
""Part UID"" TEXT, 
""Description"" TEXT, 
""Manufacturer"" TEXT, 
""MPN"" TEXT, 
""Value"" TEXT, 
""Datasheet"" TEXT,
""Schematic Symbol"" TEXT,
""Footprints"" TEXT,
""Exclude from BOM"" TEXT,
""Exclude from Board"" TEXT,
""Exclude from Sim"" TEXT,"
);

                            // Already in order as InheritedAndNormalParameters already does a ParentLibrary.AllParameters.Intersect(...)
                            // Cache a local copy of category.InheritedAndNormalParameters as we use it multiple times and it's an expensive property to get
                            var categoryParametersInOrder = category.InheritedAndNormalParameters;
                            foreach (var parameter in category.InheritedAndNormalParameters)
                            {
                                jsonKiCadDbl_Library.jsonKiCadDbl_Library_Fields.Add(new(parameter.Name, parameter.Name, true, true));
                                // Can't use prepared statements for column titles, so use .Replace("\"", "\"\"") to escape any potential quotes (even though we controlled the input)
                                createTableSqlStringBuilder.AppendFormat("\"{0}\" TEXT, ", parameter.Name.Replace("\"", "\"\""));
                            }

                            createTableSqlStringBuilder.Remove(createTableSqlStringBuilder.Length - 2, 2);
                            createTableSqlStringBuilder.Append(')');

                            createTableCommand.CommandText = createTableSqlStringBuilder.ToString();
                            createTableCommand.ExecuteNonQuery();

                            if (category.Parts.Count > 0)
                            {
                                var insertPartCommand = connection.CreateCommand();
                                StringBuilder insertPartSqlStringBuilder = new();
                                // Can't use prepared statements for column titles, so use .Replace("\"", "\"\"") to escape any potential quotes (even though we controlled the input)
                                insertPartSqlStringBuilder.Append($@"
INSERT INTO ""{tableName.Replace("\"", "\"\"")}""
VALUES (
$part_uid,
$description,
$manufacturer,
$mpn,
$value,
$datasheet,
$schematic_symbol,
$footprints,
$exclude_from_bom,
$exclude_from_board,
$exclude_from_sim, "
);

                                var partUIDParameter = insertPartCommand.CreateParameter();
                                partUIDParameter.ParameterName = "$part_uid";
                                insertPartCommand.Parameters.Add(partUIDParameter);

                                var descriptionParameter = insertPartCommand.CreateParameter();
                                descriptionParameter.ParameterName = "$description";
                                insertPartCommand.Parameters.Add(descriptionParameter);

                                var manufacturerParameter = insertPartCommand.CreateParameter();
                                manufacturerParameter.ParameterName = "$manufacturer";
                                insertPartCommand.Parameters.Add(manufacturerParameter);

                                var mpnParameter = insertPartCommand.CreateParameter();
                                mpnParameter.ParameterName = "$mpn";
                                insertPartCommand.Parameters.Add(mpnParameter);

                                var valueParameter = insertPartCommand.CreateParameter();
                                valueParameter.ParameterName = "$value";
                                insertPartCommand.Parameters.Add(valueParameter);

                                var datasheetParameter = insertPartCommand.CreateParameter();
                                datasheetParameter.ParameterName = "$datasheet";
                                insertPartCommand.Parameters.Add(datasheetParameter);

                                var schematicSymbolParameter = insertPartCommand.CreateParameter();
                                schematicSymbolParameter.ParameterName = "$schematic_symbol";
                                insertPartCommand.Parameters.Add(schematicSymbolParameter);

                                var footprintsParameter = insertPartCommand.CreateParameter();
                                footprintsParameter.ParameterName = "$footprints";
                                insertPartCommand.Parameters.Add(footprintsParameter);

                                var excludeFromBomParameter = insertPartCommand.CreateParameter();
                                excludeFromBomParameter.ParameterName = "$exclude_from_bom";
                                insertPartCommand.Parameters.Add(excludeFromBomParameter);

                                var excludeFromBoardParameter = insertPartCommand.CreateParameter();
                                excludeFromBoardParameter.ParameterName = "$exclude_from_board";
                                insertPartCommand.Parameters.Add(excludeFromBoardParameter);

                                var excludeFromSimParameter = insertPartCommand.CreateParameter();
                                excludeFromSimParameter.ParameterName = "$exclude_from_sim";
                                insertPartCommand.Parameters.Add(excludeFromSimParameter);

                                List<SqliteParameter> partParameterParameters = new();
                                for (int i = 0; i < categoryParametersInOrder.Count; i++)
                                {
                                    insertPartSqlStringBuilder.AppendFormat("$param_{0}, ", i);

                                    var partParameterParameter = insertPartCommand.CreateParameter();
                                    partParameterParameter.ParameterName = $"$param_{i}";
                                    insertPartCommand.Parameters.Add(partParameterParameter);
                                    partParameterParameters.Add(partParameterParameter);
                                }

                                insertPartSqlStringBuilder.Remove(insertPartSqlStringBuilder.Length - 2, 2);
                                insertPartSqlStringBuilder.Append(')');
                                insertPartCommand.CommandText = insertPartSqlStringBuilder.ToString();

                                // Doesn't actually seem to affect performance, but adding for completeness
                                insertPartCommand.Prepare();

                                foreach (Part part in category.Parts.OrderBy(p => p.PartUID))
                                {
                                    partUIDParameter.Value = part.PartUID;
                                    descriptionParameter.Value = part.Description;
                                    manufacturerParameter.Value = part.Manufacturer;
                                    mpnParameter.Value = part.MPN;
                                    valueParameter.Value = part.Value;
                                    // Best way I can come up with for checking for web URL and absolute-ing the file-based datasheet paths
                                    if (string.IsNullOrWhiteSpace(part.Datasheet))
                                        datasheetParameter.Value = "";
                                    else if (part.Datasheet.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                        part.Datasheet.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                                        datasheetParameter.Value = part.Datasheet;
                                    else
                                        datasheetParameter.Value = Path.Combine(this.KiCadExportPartLibraryEnvironmentVariable, part.Datasheet);
                                    schematicSymbolParameter.Value = (part.SymbolLibraryName != "" || part.SymbolName != "") ? $"{part.SymbolLibraryName}:{part.SymbolName}" : "";
                                    footprintsParameter.Value = $"{string.Join(';', part.FootprintPairs.Select(pair => $"{pair.Item1}:{pair.Item2}"))}";
                                    excludeFromBomParameter.Value = part.ExcludeFromBOM ? 1 : 0;
                                    excludeFromBoardParameter.Value = part.ExcludeFromBoard ? 1 : 0;
                                    excludeFromSimParameter.Value = part.ExcludeFromSim ? 1 : 0;
                                    for (int i = 0; i < categoryParametersInOrder.Count; i++)
                                        partParameterParameters[i].Value = part.ParameterValues[categoryParametersInOrder[i]];

                                    insertPartCommand.ExecuteNonQuery();
                                }
                            }
                        }

                        transaction.Commit();
                    }
                }
                SqliteConnection.ClearAllPools();

                if (!jsonKiCadDblFile.WriteToFile(tempDbConfPath)) return false;

                File.Copy(tempDbConfPath, kiCadDbConfFilePath, overwrite: true);
                File.Copy(tempSqlitePath, kiCadSqliteFilePath, overwrite: true);

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
