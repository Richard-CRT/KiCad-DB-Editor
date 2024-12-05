using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

                if (
                    dbPartColumnNames.Count < 8 ||
                    dbPartColumnTypes[0] != typeof(string) ||
                    dbPartColumnNames[0] != "Category" ||
                    dbPartColumnTypes[1] != typeof(string) ||
                    dbPartColumnNames[1] != "Part UID" ||
                    dbPartColumnTypes[2] != typeof(string) ||
                    dbPartColumnNames[2] != "Description" ||
                    dbPartColumnTypes[3] != typeof(string) ||
                    dbPartColumnNames[3] != "Manufacturer" ||
                    dbPartColumnTypes[4] != typeof(string) ||
                    dbPartColumnNames[4] != "MPN" ||
                    dbPartColumnTypes[5] != typeof(string) ||
                    dbPartColumnNames[5] != "Value" ||
                    dbPartColumnTypes[6] != typeof(Int64) ||
                    dbPartColumnNames[6] != "Exclude from BOM" ||
                    dbPartColumnTypes[7] != typeof(Int64) ||
                    dbPartColumnNames[7] != "Exclude from Board"
                    )
                    throw new InvalidDataException("Special columns not found or wrong type");
                for (int i = 8; i < dbPartColumnTypes.Count; i++)
                {
                    if (dbPartColumnTypes[i] != typeof(string))
                        throw new InvalidDataException($"Columns {i} wrong type");
                }

                //Dictionary<int, Parameter> columnIndexToParameterMap = new();
                Dictionary<Parameter, int> parameterToColumnIndexToMap = new();
                for (int i = 8; i < dbPartColumnNames.Count; i++)
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

                    string partUID = (string)dbPart[1];
                    Part part = new(partUID);
                    part.Description = (string)dbPart[2];
                    part.Manufacturer = (string)dbPart[3];
                    part.MPN = (string)dbPart[4];
                    part.Value = (string)dbPart[5];
                    part.ExcludeFromBOM = (Int64)dbPart[6] == 1 ? true : false;
                    part.ExcludeFromBoard = (Int64)dbPart[7] == 1 ? true : false;
                    foreach (Parameter p in partCategory.Parameters)
                    {
                        object value = dbPart[parameterToColumnIndexToMap[p]];
                        if (value is System.DBNull)
                            part.ParameterValues[p] = "";
                        else
                            part.ParameterValues[p] = (string)value;
                    }
                    /*
                    for (int i = 8; i < dbPart.Count; i++)
                    {
                        if (dbPart[i] is not System.DBNull)
                            part.ParameterValues[columnIndexToParameterMap[i]] = (string)dbPart[i];
                    }
                    */
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

        [JsonPropertyName("part_uid_scheme"), JsonPropertyOrder(1)]
        public string PartUIDScheme { get; set; } = "CMP-#####-#####";

        [JsonPropertyName("parameters"), JsonPropertyOrder(2)]
        public List<Model.Parameter> Parameters { get; set; } = new();

        [JsonPropertyName("top_level_categories"), JsonPropertyOrder(3)]
        public List<Model.Category> TopLevelCategories { get; set; } = new();

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

                string componentsFilePath = Path.Combine(projectDirectory, projectName);
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
                        "\"Exclude from Board\" INTEGER, ";
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
                        "\"Exclude from Board\", ";
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
                                $"{(part.ExcludeFromBoard ? 1 : 0)}, ";
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
