using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor.Model
{
    public class SubLibrary
    {
        [JsonPropertyName("sublibraries"), JsonPropertyOrder(2)]
        public List<Model.SubLibrary> SubLibraries { get; set; } = new();

        [JsonPropertyName("parameters"), JsonPropertyOrder(1)]
        public List<Model.Parameter> Parameters { get; set; } = new();

        [JsonPropertyName("name"), JsonPropertyOrder(0)]
        public string Name { get; set; } = "default";

        public SubLibrary()
        {
        }

        internal SubLibrary(string name)
        {
            Name = name;
        }
    }
}
