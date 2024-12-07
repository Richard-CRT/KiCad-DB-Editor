using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
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

namespace KiCAD_DB_Editor.Model
{
    public class Library
    {
        public static Library FromScratch()
        {
            Library library = new();

            return library;
        }

        public static Library FromFile(string projectFilePath)
        {
            Library library;
            try
            {
                string? projectDirectory = Path.GetDirectoryName(projectFilePath);
                string? projectName = Path.GetFileNameWithoutExtension(projectFilePath);
                if (projectDirectory is null || projectDirectory == "" || projectName is null || projectName == "")
                    throw new InvalidOperationException();

                string componentsFilePath = Path.Combine(projectDirectory, projectName);
                componentsFilePath += ".sqlite3";

                var jsonString = File.ReadAllText(projectFilePath);

                Library? o;
                o = (Library?)JsonSerializer.Deserialize(jsonString, typeof(Library), new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve });
                //o = (Library?)JsonSerializer.Deserialize(jsonString, typeof(Library), new JsonSerializerOptions { });

                if (o is null) throw new ArgumentNullException("Library is null");

                library = (Library)o!;

                library.ProjectDirectoryPath = projectDirectory;
                library.ProjectName = projectName;

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

                const int numberSpecialColumns = 11;
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
                for (int i = numberSpecialColumns; dbPartColumnNames[i].StartsWith("Footprint"); i++, numberOfFootprintColumns++) ;
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

                    bool match = false;
                    foreach (Parameter parameter in library.Parameters)
                    {
                        if (parameter.Name == columnName)
                        {
                            //columnIndexToParameterMap[i] = parameter;
                            parameterToColumnIndexToMap[parameter] = i;
                            match = true;
                            break;
                        }
                    }
                    if (!match)
                        throw new InvalidDataException("Could not find a parameter to correspond to database column name");
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
                    Part part = new(partUID);
                    part.Description = (string)dbPart[j++];
                    part.Manufacturer = (string)dbPart[j++];
                    part.MPN = (string)dbPart[j++];
                    part.Value = (string)dbPart[j++];
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
                            part.FootprintLibraryNames.Add((string)footprintLibraryNameValue);
                            part.FootprintNames.Add((string)footprintNameValue);
                        }
                    }
                    foreach (Parameter p in partCategory.Parameters)
                    {
                        object value = dbPart[parameterToColumnIndexToMap[p]];
                        // Default value to "" if parameter is expected to be present but is null
                        if (value is System.DBNull)
                            part.ParameterValues[p] = "";
                        else
                            part.ParameterValues[p] = (string)value;
                    }
                    library.Parts.Add(part);
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

        [JsonIgnore]
        public string ProjectDirectoryPath { get; set; } = "";

        [JsonIgnore]
        public string ProjectName { get; set; } = "";

        [JsonPropertyName("part_uid_scheme"), JsonPropertyOrder(1)]
        public string PartUIDScheme { get; set; } = "CMP-#######-####";

        [JsonPropertyName("parameters"), JsonPropertyOrder(2)]
        public List<Model.Parameter> Parameters { get; set; } = new();

        [JsonPropertyName("top_level_categories"), JsonPropertyOrder(3)]
        public List<Model.Category> TopLevelCategories { get; set; } = new();

        [JsonPropertyName("kicad_symbol_libraries"), JsonPropertyOrder(4)]
        public List<Model.KiCADSymbolLibrary> KiCADSymbolLibraries { get; set; } = new();

        [JsonIgnore]
        public List<Model.Part> Parts { get; set; } = new();

        public bool WriteToFile(string projectFilePath, bool autosave = false)
        {
            try
            {
                string? projectDirectory = Path.GetDirectoryName(projectFilePath);
                string? projectName = Path.GetFileNameWithoutExtension(projectFilePath);

                if (projectDirectory is null || projectDirectory == "" || projectName is null || projectName == "")
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

                File.WriteAllText(tempProjectPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true, ReferenceHandler = ReferenceHandler.Preserve }));
                //File.WriteAllText(tempProjectPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));

                List<Category> allCategories = new();
                Dictionary<Category, string> categoryToCategoryStringMap = new();
                allCategories.AddRange(TopLevelCategories);
                foreach (Category category in TopLevelCategories)
                    categoryToCategoryStringMap[category] = $"/{category.Name}";
                int i = 0;
                while (i < allCategories.Count)
                {
                    allCategories.AddRange(allCategories[i].Categories);
                    foreach (Category category in allCategories[i].Categories)
                        categoryToCategoryStringMap[category] = $"{categoryToCategoryStringMap[allCategories[i]]}/{category.Name}";
                    i++;
                }

                Dictionary<Part, string> partToCategoryStringMap = new();
                foreach (Category category in allCategories)
                {
                    foreach (Part part in category.Parts)
                    {
                        if (!partToCategoryStringMap.ContainsKey(part))
                            partToCategoryStringMap[part] = categoryToCategoryStringMap[category];
                        else
                            throw new InvalidOperationException("Part exists in multiple categories");
                    }
                }

                int maxFootprintLibraryNames = 0;
                int maxFootprintNames = 0;
                foreach (Part part in Parts)
                {
                    if (part.FootprintLibraryNames.Count != part.FootprintNames.Count)
                        throw new InvalidOperationException("Part FootprintLibraryNames doesn't match FootprintNames");

                    maxFootprintLibraryNames = Math.Max(maxFootprintLibraryNames, part.FootprintLibraryNames.Count);
                    maxFootprintNames = Math.Max(maxFootprintNames, part.FootprintNames.Count);
                }
                int maxFootprints = maxFootprintLibraryNames;

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
                    foreach (Parameter parameter in Parameters)
                        createTableSql += $"\"{parameter.Name.Replace("\"", "\"\"")}\" TEXT, ";
                    createTableSql = createTableSql[..^2];
                    createTableSql += ")";

                    var createTableCommand = connection.CreateCommand();
                    createTableCommand.CommandText = createTableSql;


                    string insertPartsSql = "INSERT INTO \"Components\" (" +
                        "\"Category\", " +
                        "\"Part UID\", " +
                        "\"Description\", " +
                        "\"Manufacturer\", " +
                        "\"MPN\", " +
                        "\"Value\", " +
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
                    foreach (Parameter parameter in Parameters)
                        insertPartsSql += $"\"{parameter.Name.Replace("\"", "\"\"")}\", ";
                    insertPartsSql = insertPartsSql[..^2];
                    insertPartsSql += ") VALUES ";
                    foreach (Part part in Parts)
                    {
                        insertPartsSql += "(" +
                                $"'{partToCategoryStringMap[part]}', " +
                                $"'{part.PartUID.Replace("'", "''")}', " +
                                $"'{part.Description.Replace("'", "''")}', " +
                                $"'{part.Manufacturer.Replace("'", "''")}', " +
                                $"'{part.MPN.Replace("'", "''")}', " +
                                $"'{part.Value.Replace("'", "''")}', " +
                                $"{(part.ExcludeFromBOM ? 1 : 0)}, " +
                                $"{(part.ExcludeFromBoard ? 1 : 0)}, " +
                                $"{(part.ExcludeFromSim ? 1 : 0)}, " +
                                $"'{part.SymbolLibraryName.Replace("'", "''")}', " +
                                $"'{part.SymbolName.Replace("'", "''")}', ";
                        for (int j = 1; j <= maxFootprints; j++)
                        {
                            // We've previously asserted part.FootprintLibraryNames.Count == part.FootprintNames.Count
                            if (j <= part.FootprintLibraryNames.Count)
                            {
                                insertPartsSql += $"'{part.FootprintLibraryNames[j - 1].Replace("'", "''")}', ";
                                insertPartsSql += $"'{part.FootprintNames[j - 1].Replace("'", "''")}', ";
                            }
                            else
                            {
                                insertPartsSql += $"NULL, NULL, ";
                            }
                        }
                        foreach (Parameter parameter in Parameters)
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

                    createTableCommand.ExecuteNonQuery();
                    insertPartsCommand.ExecuteNonQuery();

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
    }
}
