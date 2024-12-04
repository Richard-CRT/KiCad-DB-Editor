using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor.Model
{
    public class Category
    {
        [JsonPropertyName("name"), JsonPropertyOrder(0)]
        public string Name { get; set; }

        [JsonPropertyName("parameters"), JsonPropertyOrder(3)]
        public List<Model.Parameter> Parameters { get; set; } = new();

        [JsonPropertyName("categories"), JsonPropertyOrder(4)]
        public List<Model.Category> Categories { get; set; } = new();

        [JsonIgnore]
        public List<Model.Part> Parts { get; set; } = new();

        [JsonConstructor]
        private Category()
        {
            Name = "";
        }

        public Category(string name)
        {
            Name = name;
        }
    }
}
