using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor.Model
{
    public class Parameter
    {
        [JsonPropertyName("uuid"), JsonPropertyOrder(0)]
        public string UUID { get; set; } = Guid.NewGuid().ToString();
        [JsonPropertyName("name"), JsonPropertyOrder(1)]
        public string Name { get; set; } = "";

        [JsonConstructor]
        public Parameter()
        {
        }
        
        public Parameter(string name)
        {
            Name = name;
        }
    }
}
