using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor.Model
{
    public class Part
    {
        public string PartUID { get; set; }
        public string Description { get; set; }
        public string Manufacturer { get; set; }
        public string MPN { get; set; }
        public string Value { get; set; }
        public bool ExcludeFromBOM { get; set; }
        public bool ExcludeFromBoard { get; set; }
        public Dictionary<Model.Parameter, string> ParameterValues { get; set; } = new();

        public Part()
        {
        }
    }
}
