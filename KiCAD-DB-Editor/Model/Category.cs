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

        public Category(string name)
        {
            Name = name;
        }
    }
}
