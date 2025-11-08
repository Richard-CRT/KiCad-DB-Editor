using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCad_DB_Editor.Model.Json
{
    public class JsonMetadata
    {
        [JsonPropertyName("version"), JsonPropertyOrder(1)]
        public string Version { get; set; } = "";

        [JsonConstructor]
        public JsonMetadata() { }
    }
}
