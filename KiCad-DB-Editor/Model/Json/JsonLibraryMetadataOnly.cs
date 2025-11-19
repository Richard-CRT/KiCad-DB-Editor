using KiCad_DB_Editor.ViewModel;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace KiCad_DB_Editor.Model.Json
{
    public class JsonLibraryMetadataOnly
    {
        public static JsonLibraryMetadataOnly FromFile(string projectFilePath)
        {
            JsonLibraryMetadataOnly jsonLibraryMetadataOnly;
            try
            {
                var jsonString = File.ReadAllText(projectFilePath);

                JsonLibraryMetadataOnly? o;
                //o = (Library?)JsonSerializer.Deserialize(jsonString, typeof(Library), new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve });
                o = (JsonLibraryMetadataOnly?)JsonSerializer.Deserialize(jsonString, typeof(JsonLibraryMetadataOnly), new JsonSerializerOptions { });

                if (o is null) throw new ArgumentNullException("Library is null");

                jsonLibraryMetadataOnly = (JsonLibraryMetadataOnly)o!;
            }
            catch (FileNotFoundException)
            {
                throw;
            }

            return jsonLibraryMetadataOnly;
        }

        // ======================================================================

        [JsonPropertyName("metadata"), JsonPropertyOrder(1)]
        public JsonMetadata? Metadata { get; set; } = null;

        [JsonConstructor]
        public JsonLibraryMetadataOnly() { }
    }
}
