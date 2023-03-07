using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor
{
    public class KiCADDBL
    {
        public static KiCADDBL FromFile(string filePath)
        {
            KiCADDBL kiCADDBL;
            try
            {
                var jsonString = File.ReadAllText(filePath);

                KiCADDBL? o;
                o = (KiCADDBL?)JsonSerializer.Deserialize(jsonString, typeof(KiCADDBL));

                if (o is null) throw new ArgumentNullException("KiCADDBL is null");

                kiCADDBL = (KiCADDBL)o!;
            }
            catch (FileNotFoundException)
            {
                throw;
            }

            return kiCADDBL;
        }

        // =================================================================================
        // KiCAD Defined *.kicad_dbl
        // =================================================================================

        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }

        // =================================================================================
        // 
        // =================================================================================

        public KiCADDBL(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
