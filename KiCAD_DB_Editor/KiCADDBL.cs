using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using System.Xml.Linq;

namespace KiCAD_DB_Editor
{
    public class KiCADDBL
    {
        public static KiCADDBL FromFile(string filePath)
        {
            KiCADDBL kiCADDBL;
            try
            {
                var jsonString = File.ReadAllText(filePath);

                KiCADDBL? o;
                o = (KiCADDBL?)JsonSerializer.Deserialize(jsonString, typeof(KiCADDBL));

                if (o is null) throw new ArgumentNullException("KiCADDBL is null");

                kiCADDBL = (KiCADDBL)o!;
            }
            catch (FileNotFoundException)
            {
                throw;
            }

            return kiCADDBL;
        }

        // =================================================================================
        // KiCAD Defined *.kicad_dbl
        // =================================================================================

        [JsonPropertyName("meta")]
        public KiCADDBL_Meta? Meta { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        [JsonPropertyName("source")]
        public KiCADDBL_Source? Source { get; set; }
        [JsonPropertyName("libraries")]
        public KiCADDBL_Library[]? Libraries { get; set; }

        // =================================================================================
        // 
        // =================================================================================

        [JsonConstructor]
        public KiCADDBL() { }

        public KiCADDBL(Library library)
        {
            Meta = new KiCADDBL_Meta(1.0);
            Name = library.Name;
            Description = library.Description;
            Source = new(library.Source);
            Libraries = library.CategoriesEncapsulated.Select(c => new KiCADDBL_Library(c)).ToArray();
        }

        public void SaveToFile(string filePath)
        {
            File.WriteAllText(filePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    public class KiCADDBL_Meta
    {
        // =================================================================================
        // KiCAD Defined *.kicad_dbl
        // =================================================================================

        [JsonPropertyName("version")]
        public double? Version { get; set; }

        [JsonConstructor]
        public KiCADDBL_Meta() { }

        public KiCADDBL_Meta(double version)
        {
            Version = version;
        }
    }

    public class KiCADDBL_Source
    {
        // =================================================================================
        // KiCAD Defined *.kicad_dbl
        // =================================================================================

        [JsonPropertyName("type")]
        public string? Type { get; set; }
        [JsonPropertyName("dsn")]
        public string? DSN { get; set; }
        [JsonPropertyName("username")]
        public string? Username { get; set; }
        [JsonPropertyName("password")]
        public string? Password { get; set; }
        [JsonPropertyName("timeout_seconds")]
        public int? TimeOutSeconds { get; set; }
        [JsonPropertyName("connection_string")]
        public string? ConnectionString { get; set; }

        // =================================================================================
        // 
        // =================================================================================

        [JsonConstructor]
        public KiCADDBL_Source() { }

        public KiCADDBL_Source(LibrarySource librarySource)
        {
            Type = librarySource.Type;
            DSN = librarySource.DSN;
            Username = librarySource.Username;
            Password = librarySource.Password;
            TimeOutSeconds = librarySource.TimeOutSeconds;
            ConnectionString = librarySource.ConnectionString;
        }
    }
    public class KiCADDBL_Library
    {
        // =================================================================================
        // KiCAD Defined *.kicad_dbl
        // =================================================================================

        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("table")]
        public string? Table { get; set; }
        [JsonPropertyName("key")]
        public string? Key { get; set; }
        [JsonPropertyName("symbols")]
        public string? Symbols { get; set; }
        [JsonPropertyName("footprints")]
        public string? Footprints { get; set; }
        [JsonPropertyName("fields")]
        public KiCADDBL_Library_Field[]? Fields { get; set; }
        [JsonPropertyName("properties")]
        public KiCADDBL_Library_Properties? Properties { get; set; }

        // =================================================================================
        // 
        // =================================================================================

        [JsonConstructor]
        public KiCADDBL_Library() { }

        public KiCADDBL_Library(Category category)
        {
            Name = category.Name;
            Table = category.TableName;
            Key = category.KeyTableColumnName;
            Symbols = category.SymbolsTableColumnName;
            Footprints = category.FootprintsTableColumnName;
            Fields = category.SymbolFieldMaps.Select(sFM => new KiCADDBL_Library_Field(sFM)).ToArray();
            Properties = new(category.SymbolBuiltInPropertiesMap);
        }
    }

    public class KiCADDBL_Library_Field
    {
        // =================================================================================
        // KiCAD Defined *.kicad_dbl
        // =================================================================================

        [JsonPropertyName("column")]
        public string? Column { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("visible_on_add"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? VisibleOnAdd { get; set; }
        [JsonPropertyName("visible_in_chooser"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? VisibleInChooser { get; set; }
        [JsonPropertyName("show_name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? ShowName { get; set; }
        [JsonPropertyName("inherit_properties"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? InheritProperties { get; set; }

        // =================================================================================
        // 
        // =================================================================================

        [JsonConstructor]
        public KiCADDBL_Library_Field() { }

        public KiCADDBL_Library_Field(SymbolFieldMap symbolFieldMap)
        {
            Column = symbolFieldMap.TableColumnName;
            Name = symbolFieldMap.SymbolFieldName;
            if (symbolFieldMap.OverrideSymbolFieldVisibleOnAdd) VisibleOnAdd = symbolFieldMap.SymbolFieldVisibleOnAdd;
            if (symbolFieldMap.OverrideSymbolFieldVisibleInChooser) VisibleInChooser = symbolFieldMap.SymbolFieldVisibleInChooser;
            if (symbolFieldMap.OverrideSymbolFieldShowName) ShowName = symbolFieldMap.SymbolFieldShowName;
            if (symbolFieldMap.OverrideSymbolFieldInheritProperties) InheritProperties = symbolFieldMap.SymbolFieldInheritProperties;
        }
    }

    public class KiCADDBL_Library_Properties
    {
        // =================================================================================
        // KiCAD Defined *.kicad_dbl
        // =================================================================================

        [JsonPropertyName("description"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }
        [JsonPropertyName("keywords"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Keywords { get; set; }
        [JsonPropertyName("exclude_from_bom"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ExcludeFromBom { get; set; }
        [JsonPropertyName("exclude_from_board"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ExcludeFromBoard { get; set; }

        // =================================================================================
        // 
        // =================================================================================

        [JsonConstructor]
        public KiCADDBL_Library_Properties() { }

        public KiCADDBL_Library_Properties(SymbolBuiltInPropertiesMap builtInPropertiesMap)
        {
            if (builtInPropertiesMap.UseSymbolDescriptionTableColumnName) Description = builtInPropertiesMap.SymbolDescriptionTableColumnName;
            if (builtInPropertiesMap.UseSymbolKeywordsTableColumnName) Keywords = builtInPropertiesMap.SymbolKeywordsTableColumnName;
            if (builtInPropertiesMap.UseSymbolExcludeFromBomTableColumnName) ExcludeFromBom = builtInPropertiesMap.SymbolExcludeFromBomTableColumnName;
            if (builtInPropertiesMap.UseSymbolExcludeFromBoardTableColumnName) ExcludeFromBoard = builtInPropertiesMap.SymbolExcludeFromBoardTableColumnName;
        }
    }
}
