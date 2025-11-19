using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCad_DB_Editor.Model.Legacy.V5.Json
{
    public class JsonKiCadFootprintLibrary
    {
        [JsonPropertyName("nickname"), JsonPropertyOrder(1)]
        public string Nickname { get; set; } = "";

        [JsonPropertyName("relative_path"), JsonPropertyOrder(2)]
        public string RelativePath { get; set; } = "";

        [JsonConstructor]
        public JsonKiCadFootprintLibrary() { }

        public JsonKiCadFootprintLibrary(KiCadFootprintLibrary kiCadFootprintLibrary)
        {
            Nickname = kiCadFootprintLibrary.Nickname;
            RelativePath = kiCadFootprintLibrary.RelativePath;
        }
    }
}
