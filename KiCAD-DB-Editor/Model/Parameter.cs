using KiCAD_DB_Editor.Model.Json;
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
        public string UUID { get; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";

        public Parameter(JsonParameter jsonParameter)
        {
            this.UUID = jsonParameter.UUID;
            this.Name = jsonParameter.Name;
        }

        public Parameter(string name)
        {
            this.UUID = Guid.NewGuid().ToString();
            this.Name = name;
        }
    }
}
