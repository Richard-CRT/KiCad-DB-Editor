using KiCAD_DB_Editor.ViewModel;
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

namespace KiCAD_DB_Editor.Model.Json
{
    public class JsonLibrary
    {
        public static JsonLibrary FromFile(string projectFilePath)
        {
            JsonLibrary jsonLibrary;
            try
            {
                string? projectDirectory = Path.GetDirectoryName(projectFilePath);
                string? projectName = Path.GetFileNameWithoutExtension(projectFilePath);
                if (projectDirectory is null || projectDirectory == "" || projectName is null || projectName == "")
                    throw new InvalidOperationException();

                string componentsFilePath = Path.Combine(projectDirectory, projectName);
                componentsFilePath += ".sqlite3";

                var jsonString = File.ReadAllText(projectFilePath);

                JsonLibrary? o;
                //o = (Library?)JsonSerializer.Deserialize(jsonString, typeof(Library), new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve });
                o = (JsonLibrary?)JsonSerializer.Deserialize(jsonString, typeof(JsonLibrary), new JsonSerializerOptions { });

                if (o is null) throw new ArgumentNullException("Library is null");

                jsonLibrary = (JsonLibrary)o!;
            }
            catch (FileNotFoundException)
            {
                throw;
            }

            return jsonLibrary;
        }

        // ======================================================================

        [JsonPropertyName("part_uid_scheme"), JsonPropertyOrder(1)]
        public string PartUIDScheme { get; set; } = "CMP-#######-####";

        [JsonPropertyName("parameters"), JsonPropertyOrder(2)]
        public List<JsonParameter> Parameters { get; set; } = new();

        [JsonPropertyName("top_level_categories"), JsonPropertyOrder(3)]
        public List<JsonCategory> TopLevelCategories { get; set; } = new();

        [JsonPropertyName("kicad_symbol_libraries"), JsonPropertyOrder(4)]
        public List<JsonKiCADSymbolLibrary> KiCADSymbolLibraries { get; set; } = new();

        [JsonPropertyName("kicad_footprint_libraries"), JsonPropertyOrder(5)]
        public List<JsonKiCADFootprintLibrary> KiCADFootprintLibraries { get; set; } = new();

        public bool WriteToFile(string projectFilePath, bool autosave = false)
        {
            try
            {
                string? projectDirectory = Path.GetDirectoryName(projectFilePath);
                string? projectName = Path.GetFileNameWithoutExtension(projectFilePath);

                if (projectDirectory is null || projectDirectory == "" || projectName is null || projectName == "")
                    throw new InvalidOperationException();

                if (autosave)
                {
                    projectFilePath += ".autosave";
                }

                string tempProjectPath = $"proj.tmp";
                string tempComponentsPath = $"components.tmp";

                //File.WriteAllText(tempProjectPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true, ReferenceHandler = ReferenceHandler.Preserve }));
                File.WriteAllText(tempProjectPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));

                File.Move(tempProjectPath, projectFilePath, overwrite: true);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
