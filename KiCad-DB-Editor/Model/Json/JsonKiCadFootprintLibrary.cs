using System.Text.Json.Serialization;

namespace KiCad_DB_Editor.Model.Json
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

        public JsonKiCadFootprintLibrary(Legacy.V5.Json.JsonKiCadFootprintLibrary jsonV5KiCadFootprintLibrary)
        {
            Nickname = jsonV5KiCadFootprintLibrary.Nickname;
            RelativePath = jsonV5KiCadFootprintLibrary.RelativePath;
        }
    }
}
