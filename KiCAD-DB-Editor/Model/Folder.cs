using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor.Model
{
    public class Folder
    {
        [JsonPropertyName("folders"), JsonPropertyOrder(2)]
        public List<Model.Folder> Folders { get; set; } = new();

        [JsonPropertyName("categories"), JsonPropertyOrder(3)]
        public List<Model.Category> Categories { get; set; } = new();

        [JsonPropertyName("name"), JsonPropertyOrder(0)]
        public string Name { get; set; }

        [JsonConstructor]
        private Folder()
        {
            Name = "";
        }

        public Folder(string name)
        {
            Name = name;
        }
    }
}
