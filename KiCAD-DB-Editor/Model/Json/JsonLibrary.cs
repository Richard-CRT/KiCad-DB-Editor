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
        public string PartUIDScheme { get; set; } = "";

        [JsonPropertyName("part_lib_name"), JsonPropertyOrder(2)]
        public string KiCadExportPartLibraryName { get; set; } = "";

        [JsonPropertyName("part_lib_desc"), JsonPropertyOrder(3)]
        public string KiCadExportPartLibraryDescription { get; set; } = "";

        [JsonPropertyName("export_odbc_name"), JsonPropertyOrder(4)]
        public string KiCadExportOdbcName { get; set; } = "";

        [JsonPropertyName("auto_export"), JsonPropertyOrder(5)]
        public bool KiCadAutoExportOnSave { get; set; } = false;

        [JsonPropertyName("auto_export_relative_path"), JsonPropertyOrder(6)]
        public string KiCadAutoExportRelativePath { get; set; } = "";

        [JsonPropertyName("parameters"), JsonPropertyOrder(7)]
        public List<JsonParameter> AllParameters { get; set; } = new();

        [JsonPropertyName("top_level_categories"), JsonPropertyOrder(8)]
        public List<JsonCategory> TopLevelCategories { get; set; } = new();

        [JsonPropertyName("kicad_symbol_libraries"), JsonPropertyOrder(9)]
        public List<JsonKiCADSymbolLibrary> KiCADSymbolLibraries { get; set; } = new();

        [JsonPropertyName("kicad_footprint_libraries"), JsonPropertyOrder(10)]
        public List<JsonKiCADFootprintLibrary> KiCADFootprintLibraries { get; set; } = new();

        [JsonConstructor]
        public JsonLibrary() { }

        public JsonLibrary(Library library)
        {
            PartUIDScheme = library.PartUIDScheme;
            KiCadExportPartLibraryName = library.KiCadExportPartLibraryName;
            KiCadExportPartLibraryDescription = library.KiCadExportPartLibraryDescription;
            KiCadExportOdbcName = library.KiCadExportOdbcName;
            KiCadAutoExportOnSave = library.KiCadAutoExportOnSave;
            KiCadAutoExportRelativePath = library.KiCadAutoExportRelativePath;
            AllParameters = new(library.AllParameters.Select(p => new JsonParameter(p)));
            TopLevelCategories = new(library.TopLevelCategories.Select(c => new JsonCategory(c)));
            KiCADSymbolLibraries = new(library.KiCADSymbolLibraries.Select(kSL => new JsonKiCADSymbolLibrary(kSL)));
            KiCADFootprintLibraries = new(library.KiCADFootprintLibraries.Select(kFL => new JsonKiCADFootprintLibrary(kFL)));
        }

        public bool WriteToFile(string filePath, bool autosave = false)
        {
            try
            {
                //File.WriteAllText(tempProjectPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true, ReferenceHandler = ReferenceHandler.Preserve }));
                File.WriteAllText(filePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
