using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor.Model
{
    public class Library
    {
        public static Library FromScratch()
        {
            Library library = new();

            return library;
        }

        public static Library FromFile(string filePath)
        {
            Library library;
            try
            {
                var jsonString = File.ReadAllText(filePath);

                Library? o;
                o = (Library?)JsonSerializer.Deserialize(jsonString, typeof(Library), new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve });
                //o = (Library?)JsonSerializer.Deserialize(jsonString, typeof(Library), new JsonSerializerOptions { });

                if (o is null) throw new ArgumentNullException("Library is null");

                library = (Library)o!;
            }
            catch (FileNotFoundException)
            {
                throw;
            }

            return library;
        }

        // ======================================================================

        [JsonPropertyName("parameters"), JsonPropertyOrder(1)]
        public List<Model.Parameter> Parameters { get; set; } = new();

        [JsonPropertyName("top_level_categories"), JsonPropertyOrder(2)]
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

                File.Delete(tempComponentsPath);
                using (var connection = new SqliteConnection($"Data Source={tempComponentsPath}"))
                {
                    connection.Open();

                    /* // Better DB Structure but harder for humans

                    string createTablesSql = """
                        CREATE TABLE "Parts" (
                            "Part UID" TEXT,
                            "Description" TEXT,
                            "Manufacturer" TEXT,
                            "MPN" TEXT,
                            "Value" TEXT,
                            "Exclude from BOM" INTEGER,
                            "Exclude from Board" INTEGER,
                            PRIMARY KEY("Part UID")
                            );
                           
                        CREATE TABLE "Parameters" (
                            "Part UID" TEXT,
                            "Parameter Name" TEXT,
                            "Value" TEXT,
                            PRIMARY KEY("Part UID", "Parameter Name")
                            );
                        """;

                    var createTablesCommand = connection.CreateCommand();
                    createTablesCommand.CommandText = createTablesSql;
                    createTablesCommand.ExecuteNonQuery();

                    foreach (Part part in Parts)
                    {
                        string insertPartSql = """
                            INSERT INTO "Parts" (
                                "Part UID",
                                "Description",
                                "Manufacturer",
                                "MPN",
                                "Value",
                                "Exclude from BOM",
                                "Exclude from Board") VALUES (
                                $partUID, $description, $manufacturer, $mpn, $value, $excludeFromBOM, $excludeFromBoard);
                            """;
                        var insertPartCommand = connection.CreateCommand();
                        insertPartCommand.CommandText = insertPartSql;
                        insertPartCommand.Parameters.AddWithValue("$partUID", part.PartUID);
                        insertPartCommand.Parameters.AddWithValue("$description", part.Description);
                        insertPartCommand.Parameters.AddWithValue("$manufacturer", part.Manufacturer);
                        insertPartCommand.Parameters.AddWithValue("$mpn", part.MPN);
                        insertPartCommand.Parameters.AddWithValue("$value", part.Value);
                        insertPartCommand.Parameters.AddWithValue("$excludeFromBOM", part.ExcludeFromBOM);
                        insertPartCommand.Parameters.AddWithValue("$excludeFromBoard", part.ExcludeFromBoard);

                        insertPartCommand.ExecuteNonQuery();
                    }


                    string insertParameterSql = """INSERT INTO "Parameters" ("Part UID", "Parameter Name", "Value") VALUES """;
                    bool anyInsertions = false;
                    Dictionary<Part, string> partToPreparedKeyMap = new();
                    Dictionary<Parameter, string> parameterToPreparedKeyMap = new();
                    Dictionary<string, string> preparedKeyToValueMap = new();
                    foreach (Part part in Parts)
                    {
                        string partUIDPrepared = $"$pUID{partToPreparedKeyMap.Count}";
                        partToPreparedKeyMap[part] = partUIDPrepared;

                        foreach ((Parameter parameter, string parameterValue) in part.ParameterValues)
                        {
                            if (!parameterToPreparedKeyMap.TryGetValue(parameter, out string? parameterPrepared))
                            {
                                parameterPrepared = $"$p{parameterToPreparedKeyMap.Count}";
                                parameterToPreparedKeyMap[parameter] = parameterPrepared;
                            }

                            string preparedValueString = $"$v{preparedKeyToValueMap.Count}";
                            preparedKeyToValueMap[preparedValueString] = parameterValue;

                            insertParameterSql += $"\n({partUIDPrepared}, {parameterPrepared}, {preparedValueString}),";
                            anyInsertions = true;
                        }
                    }

                    if (anyInsertions)
                    {
                        insertParameterSql = insertParameterSql[..^1];
                        var insertParameterCommand = connection.CreateCommand();
                        insertParameterCommand.CommandText = insertParameterSql;
                        foreach ((Part part, string preparedKey) in partToPreparedKeyMap)
                            insertParameterCommand.Parameters.AddWithValue(preparedKey, part.PartUID);
                        foreach ((Parameter parameter, string preparedKey) in parameterToPreparedKeyMap)
                            insertParameterCommand.Parameters.AddWithValue(preparedKey, parameter.Name);
                        foreach ((string preparedKey, string value) in preparedKeyToValueMap)
                            insertParameterCommand.Parameters.AddWithValue(preparedKey, value);

                        insertParameterCommand.ExecuteNonQuery();
                    }
                    */

                    // Worse DB structure but simpler for humans
                    string createTableCommandString = "CREATE TABLE \"Components\" (" +
                        "\"Part UID\" TEXT, " +
                        "\"Description\" TEXT, " +
                        "\"Manufacturer\" TEXT, " +
                        "\"MPN\" TEXT, " +
                        "\"Value\" TEXT, " +
                        "\"Exclude from BOM\" INTEGER, " +
                        "\"Exclude from Board\" INTEGER, ";
                    foreach (Parameter parameter in Parameters)
                        createTableCommandString += $"\"{parameter.Name.Replace("\"", "\"\"")}\" TEXT, ";
                    createTableCommandString = createTableCommandString[..^2];
                    createTableCommandString += ")";

                    var createTableCommand = connection.CreateCommand();
                    createTableCommand.CommandText = createTableCommandString;


                    string insertPartsCommandString = "INSERT INTO \"Components\" (" +
                        "\"Part UID\", " +
                        "\"Description\", " +
                        "\"Manufacturer\", " +
                        "\"MPN\", " +
                        "\"Value\", " +
                        "\"Exclude from BOM\", " +
                        "\"Exclude from Board\", ";
                    foreach (Parameter parameter in Parameters)
                        insertPartsCommandString += $"\"{parameter.Name.Replace("\"", "\"\"")}\", ";
                    insertPartsCommandString = insertPartsCommandString[..^2];
                    insertPartsCommandString += ") VALUES ";
                    foreach (Part part in Parts)
                    {
                        insertPartsCommandString += "(" +
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
                                insertPartsCommandString += $"'{value.Replace("'", "''")}', ";
                            else
                                insertPartsCommandString += $"NULL, ";
                        }
                        insertPartsCommandString = insertPartsCommandString[..^2];
                        insertPartsCommandString += "), ";
                    }
                    insertPartsCommandString = insertPartsCommandString[..^2];

                    var insertPartsCommand = connection.CreateCommand();
                    insertPartsCommand.CommandText = insertPartsCommandString;

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
