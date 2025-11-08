using System.Data;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KiCad_DB_Editor.Model.Json
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

                var jsonString = File.ReadAllText(projectFilePath);

                JsonLibrary? o;
                //o = (Library?)JsonSerializer.Deserialize(jsonString, typeof(Library), new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve });
                o = (JsonLibrary?)JsonSerializer.Deserialize(jsonString, typeof(JsonLibrary), new JsonSerializerOptions { });

                if (o is null) throw new ArgumentNullException("Library is null");

                jsonLibrary = o!;
            }
            catch (FileNotFoundException)
            {
                throw;
            }

            return jsonLibrary;
        }

        // ======================================================================

        [JsonPropertyName("metadata"), JsonPropertyOrder(1)]
        public JsonMetadata? Metadata { get; set; } = null;

        [JsonPropertyName("part_uid_scheme"), JsonPropertyOrder(2)]
        public string PartUIDScheme { get; set; } = "";

        [JsonPropertyName("part_lib_name"), JsonPropertyOrder(3)]
        public string KiCadExportPartLibraryName { get; set; } = "";

        [JsonPropertyName("part_lib_desc"), JsonPropertyOrder(4)]
        public string KiCadExportPartLibraryDescription { get; set; } = "";

        [JsonPropertyName("export_odbc_name"), JsonPropertyOrder(5)]
        public string KiCadExportOdbcName { get; set; } = "";

        [JsonPropertyName("kicad_part_lib_env_var"), JsonPropertyOrder(6)]
        public string KiCadExportPartLibraryEnvironmentVariable { get; set; } = "";

        [JsonPropertyName("auto_export"), JsonPropertyOrder(7)]
        public bool KiCadAutoExportOnSave { get; set; } = false;

        [JsonPropertyName("auto_export_relative_path"), JsonPropertyOrder(8)]
        public string KiCadAutoExportRelativePath { get; set; } = "";

        [JsonPropertyName("universal_parameters"), JsonPropertyOrder(9)]
        public List<string> UniversalParameters { get; set; } = new();

        [JsonPropertyName("top_level_categories"), JsonPropertyOrder(10)]
        public List<JsonCategory> TopLevelCategories { get; set; } = new();

        [JsonPropertyName("kicad_symbol_libraries"), JsonPropertyOrder(11)]
        public List<JsonKiCadSymbolLibrary> KiCadSymbolLibraries { get; set; } = new();

        [JsonPropertyName("kicad_footprint_libraries"), JsonPropertyOrder(12)]
        public List<JsonKiCadFootprintLibrary> KiCadFootprintLibraries { get; set; } = new();

        [JsonConstructor]
        public JsonLibrary() { }

        public JsonLibrary(Library library)
        {
            Metadata = new JsonMetadata();
            Metadata.Version = Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
            PartUIDScheme = library.PartUIDScheme;
            KiCadExportPartLibraryName = library.KiCadExportPartLibraryName;
            KiCadExportPartLibraryDescription = library.KiCadExportPartLibraryDescription;
            KiCadExportOdbcName = library.KiCadExportOdbcName;
            KiCadExportPartLibraryEnvironmentVariable = library.KiCadExportPartLibraryEnvironmentVariable;
            KiCadAutoExportOnSave = library.KiCadAutoExportOnSave;
            KiCadAutoExportRelativePath = library.KiCadAutoExportRelativePath;
            UniversalParameters = new(library.UniversalParameters);
            TopLevelCategories = new(library.TopLevelCategories.Select(c => new JsonCategory(c)));
            KiCadSymbolLibraries = new(library.KiCadSymbolLibraries.Select(kSL => new JsonKiCadSymbolLibrary(kSL)));
            KiCadFootprintLibraries = new(library.KiCadFootprintLibraries.Select(kFL => new JsonKiCadFootprintLibrary(kFL)));
        }

        public JsonLibrary(Legacy.V5.Json.JsonLibrary jsonV5Library)
        {
            Metadata = new JsonMetadata();
            Metadata.Version = Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
            PartUIDScheme = jsonV5Library.PartUIDScheme;
            KiCadExportPartLibraryName = jsonV5Library.KiCadExportPartLibraryName;
            KiCadExportPartLibraryDescription = jsonV5Library.KiCadExportPartLibraryDescription;
            KiCadExportOdbcName = jsonV5Library.KiCadExportOdbcName;
            KiCadExportPartLibraryEnvironmentVariable = jsonV5Library.KiCadExportPartLibraryEnvironmentVariable;
            KiCadAutoExportOnSave = jsonV5Library.KiCadAutoExportOnSave;
            KiCadAutoExportRelativePath = jsonV5Library.KiCadAutoExportRelativePath;
            TopLevelCategories = new(jsonV5Library.TopLevelCategories.Select(c => new JsonCategory(jsonV5Library, c)));
            KiCadSymbolLibraries = new(jsonV5Library.KiCadSymbolLibraries.Select(kCSL => new JsonKiCadSymbolLibrary(kCSL)));
            KiCadFootprintLibraries = new(jsonV5Library.KiCadFootprintLibraries.Select(kCFL => new JsonKiCadFootprintLibrary(kCFL)));

            // This has changed
            UniversalParameters = new(jsonV5Library.AllParameters.Where(p => p.Universal).Select(p => p.Name));
        }

        public void WriteToFile(string filePath, bool autosave = false)
        {
            //File.WriteAllText(tempProjectPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true, ReferenceHandler = ReferenceHandler.Preserve }));
            File.WriteAllText(filePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
