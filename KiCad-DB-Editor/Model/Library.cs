﻿using KiCad_DB_Editor.Commands;
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
using System.Data.Common;

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

        private void _allParameters_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var c in AllCategories) c.ParentLibrary_AllParameters_CollectionChanged(sender, e);
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

                string componentsFilePath = Path.Combine(this.ProjectDirectoryPath, this.ProjectName);
                componentsFilePath += ".sqlite3";

                if (autosave)
                {
                    projectFilePath += ".autosave";
                    componentsFilePath += ".autosave";
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
                        // Worse DB structure but simpler for humans

                        var createTableCommand = connection.CreateCommand();
                        StringBuilder createTableSqlStringBuilder = new();
                        createTableSqlStringBuilder.Append(@"
CREATE TABLE ""Components"" (
""Category"" TEXT,
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
""Symbol Name"" TEXT,"
);

                        for (int i = 1; i <= maxFootprints; i++)
                            createTableSqlStringBuilder.AppendFormat("\"Footprint {0} Library Name\" TEXT, \"Footprint {0} Name\" TEXT, ", i);
                        foreach (Parameter parameter in AllParameters)
                            // Can't use prepared statements for column titles, so use .Replace("\"", "\"\"") to escape any potential quotes (even though we controlled the input)
                            createTableSqlStringBuilder.AppendFormat("\"{0} {1}\" TEXT, ", parameter.Name.Replace("\"", "\"\""), parameter.UUID);

                        createTableSqlStringBuilder.Remove(createTableSqlStringBuilder.Length - 2, 2);
                        createTableSqlStringBuilder.Append(')');

                        createTableCommand.CommandText = createTableSqlStringBuilder.ToString();
                        createTableCommand.ExecuteNonQuery();

                        if (AllParts.Count > 0)
                        {
                            var insertPartCommand = connection.CreateCommand();
                            StringBuilder insertPartSqlStringBuilder = new();
                            insertPartSqlStringBuilder.Append(@"
INSERT INTO ""Components""
VALUES (
$category_string,
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
$symbol_name, "
);

                            var categoryStringParameter = insertPartCommand.CreateParameter();
                            categoryStringParameter.ParameterName = "$category_string";
                            insertPartCommand.Parameters.Add(categoryStringParameter);

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

                            var excludeFromBomParameter = insertPartCommand.CreateParameter();
                            excludeFromBomParameter.ParameterName = "$exclude_from_bom";
                            insertPartCommand.Parameters.Add(excludeFromBomParameter);

                            var excludeFromBoardParameter = insertPartCommand.CreateParameter();
                            excludeFromBoardParameter.ParameterName = "$exclude_from_board";
                            insertPartCommand.Parameters.Add(excludeFromBoardParameter);

                            var excludeFromSimParameter = insertPartCommand.CreateParameter();
                            excludeFromSimParameter.ParameterName = "$exclude_from_sim";
                            insertPartCommand.Parameters.Add(excludeFromSimParameter);

                            var symbolLibNameParameter = insertPartCommand.CreateParameter();
                            symbolLibNameParameter.ParameterName = "$symbol_lib_name";
                            insertPartCommand.Parameters.Add(symbolLibNameParameter);

                            var symbolNameParameter = insertPartCommand.CreateParameter();
                            symbolNameParameter.ParameterName = "$symbol_name";
                            insertPartCommand.Parameters.Add(symbolNameParameter);

                            List<SqliteParameter> footprintLibNameParameters = new();
                            List<SqliteParameter> footprintNameParameters = new();
                            for (int i = 0; i < maxFootprints; i++)
                            {
                                insertPartSqlStringBuilder.AppendFormat("$footprint_lib_name_{0}, $footprint_name_{0}, ", i);

                                var footprintLibNameParameter = insertPartCommand.CreateParameter();
                                footprintLibNameParameter.ParameterName = $"$footprint_lib_name_{i}";
                                insertPartCommand.Parameters.Add(footprintLibNameParameter);
                                footprintLibNameParameters.Add(footprintLibNameParameter);

                                var footprintNameParameter = insertPartCommand.CreateParameter();
                                footprintNameParameter.ParameterName = $"$footprint_name_{i}";
                                insertPartCommand.Parameters.Add(footprintNameParameter);
                                footprintNameParameters.Add(footprintNameParameter);
                            }

                            List<SqliteParameter> partParameterParameters = new();
                            for (int i = 0; i < AllParameters.Count; i++)
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

                            foreach (Part part in AllParts.OrderBy(p => p.PartUID))
                            {
                                categoryStringParameter.Value = categoryToCategoryStringMap[part.ParentCategory];
                                partUIDParameter.Value = part.PartUID;
                                descriptionParameter.Value = part.Description;
                                manufacturerParameter.Value = part.Manufacturer;
                                mpnParameter.Value = part.MPN;
                                valueParameter.Value = part.Value;
                                datasheetParameter.Value = part.Datasheet;
                                excludeFromBomParameter.Value = part.ExcludeFromBOM;
                                excludeFromBoardParameter.Value = part.ExcludeFromBoard;
                                excludeFromSimParameter.Value = part.ExcludeFromSim;
                                symbolLibNameParameter.Value = part.SymbolLibraryName;
                                symbolNameParameter.Value = part.SymbolName;
                                for (int i = 0; i < maxFootprints; i++)
                                {
                                    if (i < part.FootprintPairs.Count)
                                    {
                                        footprintLibNameParameters[i].Value = part.FootprintPairs[i].Item1;
                                        footprintNameParameters[i].Value = part.FootprintPairs[i].Item2;
                                    }
                                    else
                                    {
                                        footprintLibNameParameters[i].Value = System.DBNull.Value;
                                        footprintNameParameters[i].Value = System.DBNull.Value;
                                    }
                                }
                                for (int i = 0; i < AllParameters.Count; i++)
                                {
                                    if (part.ParameterValues.TryGetValue(AllParameters[i], out string? value))
                                        partParameterParameters[i].Value = value;
                                    else
                                        partParameterParameters[i].Value = System.DBNull.Value;
                                }

                                insertPartCommand.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                }
                SqliteConnection.ClearAllPools();

                File.Copy(tempProjectPath, projectFilePath, overwrite: true);
                File.Copy(tempDataPath, componentsFilePath, overwrite: true);

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
