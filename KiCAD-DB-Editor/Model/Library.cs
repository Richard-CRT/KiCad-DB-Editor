using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor.Model
{
    public class Library
    {
        public static Library FromFile(string filePath)
        {
            Library library;
            try
            {
                var jsonString = File.ReadAllText(filePath);

                Library? o;
                o = (Library?)JsonSerializer.Deserialize(jsonString, typeof(Library), new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve });

                if (o is null) throw new ArgumentNullException("Library is null");

                library = (Library)o!;
            }
            catch (FileNotFoundException)
            {
                throw;
            }

            return library;
        }

        // ======================================================================

        [JsonPropertyName("parameters"), JsonPropertyOrder(1)]
        public List<Model.Parameter> Parameters { get; set; } = new();

        [JsonPropertyName("top_level_folder"), JsonPropertyOrder(2)]
        public Folder TopLevelFolder { get; set; } = new("Components");

        public void WriteToFile(string filePath)
        {
            string tempPath = $"temp.tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true, ReferenceHandler = ReferenceHandler.Preserve }));
            File.Move(tempPath, filePath, overwrite: true);
        }
    }
}
