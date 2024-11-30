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
        [JsonPropertyName("name")]
        public string Name { get; set; }

        public Parameter(string name)
        {
            Name = name;
        }
    }
}
