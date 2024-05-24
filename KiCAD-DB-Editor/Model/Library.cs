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
                o = (Library?)JsonSerializer.Deserialize(jsonString, typeof(Library), new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles });

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

        [JsonPropertyName("top_level_sublibrary")]
        public SubLibrary TopLevelSubLibrary { get; set; } = new("Components");

        public void WriteToFile(string filePath)
        {
            string tempPath = $"temp.tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true, ReferenceHandler = ReferenceHandler.IgnoreCycles }));
            File.Move(tempPath, filePath, overwrite: true);
        }
    }
}
