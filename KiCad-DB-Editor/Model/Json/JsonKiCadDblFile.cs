using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCad_DB_Editor.Model.Json
{
    public class JsonKiCadDblFile
    {
        [JsonPropertyName("meta"), JsonPropertyOrder(1)]
        public Dictionary<string, int> meta { get; set; }
        [JsonPropertyName("name"), JsonPropertyOrder(2)]
        public string PartLibName { get; set; }
        [JsonPropertyName("description"), JsonPropertyOrder(3)]
        public string PartLibDescription { get; set; }
        [JsonPropertyName("source"), JsonPropertyOrder(4)]
        public JsonKiCadDbl_Source Source { get; set; }
        [JsonPropertyName("libraries"), JsonPropertyOrder(5)]
        public List<JsonKiCadDbl_Library> jsonKiCadDbl_Libraries { get; set; }

        public JsonKiCadDblFile(string partLibName, string partLibDescription, string odbcName)
        {
            meta = new Dictionary<string, int> { { "version", 0 } };
            PartLibName = partLibName;
            PartLibDescription = partLibDescription;
            Source = new JsonKiCadDbl_Source(odbcName);
            jsonKiCadDbl_Libraries = new();
        }

        public bool WriteToFile(string filePath)
        {
            try
            {
                File.WriteAllText(filePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
    public class JsonKiCadDbl_Source
    {
        [JsonPropertyName("type"), JsonPropertyOrder(1)]
        public string Type { get; set; }
        [JsonPropertyName("dsn"), JsonPropertyOrder(2)]
        public string Dsn { get; set; }
        [JsonPropertyName("username"), JsonPropertyOrder(3)]
        public string Username { get; set; }
        [JsonPropertyName("password"), JsonPropertyOrder(4)]
        public string Password { get; set; }
        [JsonPropertyName("timeout_seconds"), JsonPropertyOrder(5)]
        public int TimeOutSeconds { get; set; }
        [JsonPropertyName("connection_string"), JsonPropertyOrder(6)]
        public string ConnectionString { get; set; }

        public JsonKiCadDbl_Source(string odbcName)
        {
            Type = "odbc";
            Dsn = "";
            Username = "";
            Password = "";
            TimeOutSeconds = 2;
            ConnectionString = $"DSN={odbcName};";
        }
    }

    public class JsonKiCadDbl_Library
    {
        [JsonPropertyName("name"), JsonPropertyOrder(1)]
        public string CategoryName { get; set; }
        [JsonPropertyName("table"), JsonPropertyOrder(2)]
        public string TableName { get; set; }
        [JsonPropertyName("key"), JsonPropertyOrder(3)]
        public string DbKeyFieldName { get; set; }
        [JsonPropertyName("symbols"), JsonPropertyOrder(4)]
        public string DbSymbolsFieldName { get; set; }
        [JsonPropertyName("footprints"), JsonPropertyOrder(5)]
        public string DbFootprintsFieldName { get; set; }
        [JsonPropertyName("fields"), JsonPropertyOrder(5)]
        public List<JsonKiCadDbl_Library_Field> jsonKiCadDbl_Library_Fields { get; set; }
        [JsonPropertyName("properties"), JsonPropertyOrder(6)]
        public Dictionary<string, string> jsonKiCadDbl_Library_Properties { get; set; }

        public JsonKiCadDbl_Library(string categoryName, string tableName, string dbKeyFieldName, string dbSymbolsFieldName, string dbFootprintsFieldName)
        {
            CategoryName = categoryName;
            TableName = tableName;
            DbKeyFieldName = dbKeyFieldName;
            DbSymbolsFieldName = dbSymbolsFieldName;
            DbFootprintsFieldName = dbFootprintsFieldName;
            jsonKiCadDbl_Library_Fields = new();
            jsonKiCadDbl_Library_Properties = new();
        }
    }

    public class JsonKiCadDbl_Library_Field
    {
        [JsonPropertyName("column"), JsonPropertyOrder(1)]
        public string DbFieldName { get; set; }
        [JsonPropertyName("name"), JsonPropertyOrder(2)]
        public string KiCadFieldName { get; set; }
        [JsonPropertyName("visible_in_chooser"), JsonPropertyOrder(3)]
        public bool FieldVisibleInChooser { get; set; }
        [JsonPropertyName("inherit_properties"), JsonPropertyOrder(4)]
        public bool InheritSymbolProperties { get; set; }

        public JsonKiCadDbl_Library_Field(string kiCadFieldName, string dbFieldName, bool fieldVisibleInChooser, bool inheritSymbolProperties)
        {
            KiCadFieldName = kiCadFieldName;
            DbFieldName = dbFieldName;
            FieldVisibleInChooser = fieldVisibleInChooser;
            InheritSymbolProperties = inheritSymbolProperties;
        }
    }
}
