using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCad_DB_Editor.Model.Legacy.V5.Json
{
    public class JsonParameter
    {
        [JsonPropertyName("uuid"), JsonPropertyOrder(0)]
        public string UUID { get; set; } = Guid.NewGuid().ToString();
        [JsonPropertyName("name"), JsonPropertyOrder(1)]
        public string Name { get; set; } = "";
        [JsonPropertyName("universal"), JsonPropertyOrder(2)]
        public bool Universal { get; set; } = false;

        [JsonConstructor]
        public JsonParameter() { }
    }
}
