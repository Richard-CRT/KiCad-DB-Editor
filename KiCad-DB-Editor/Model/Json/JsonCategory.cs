using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCad_DB_Editor.Model.Json
{
    public class JsonCategory
    {
        [JsonPropertyName("name"), JsonPropertyOrder(0)]
        public string Name { get; set; } = "";

        [JsonPropertyName("parameters"), JsonPropertyOrder(3)]
        public List<string> Parameters { get; set; } = new();

        [JsonPropertyName("categories"), JsonPropertyOrder(4)]
        public List<JsonCategory> Categories { get; set; } = new();

        [JsonConstructor]
        private JsonCategory()
        {
        }

        public JsonCategory(Category category)
        {
            Name = category.Name;
            Parameters = new(category.Parameters);
            Categories = new(category.Categories.Select(c => new JsonCategory(c)));
        }

        public JsonCategory(Legacy.V5.Json.JsonLibrary jsonV5Library, Legacy.V5.Json.JsonCategory jsonV5Category)
        {
            Name = jsonV5Category.Name;
            Categories = new(jsonV5Category.Categories.Select(c => new JsonCategory(jsonV5Library, c)));

            // This has changed
            Parameters = new(jsonV5Category.Parameters.Select(uuid => jsonV5Library.AllParameters.First(p => p.UUID == uuid).Name));
        }
    }
}
