using KiCad_DB_Editor.Model.Legacy.V5.Json;
using KiCad_DB_Editor.Model.Loaders;
using KiCad_DB_Editor.ViewModel;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Reflection.Metadata;

namespace KiCad_DB_Editor.Model.Legacy.V5
{

    public class Upgrader : IUpgrader
    {
        public static void Upgrade(string projectFilePath)
        {
            string? projectDirectory = Path.GetDirectoryName(projectFilePath);
            string? projectName = Path.GetFileNameWithoutExtension(projectFilePath);
            if (projectDirectory is null || projectDirectory == "" || projectName is null || projectName == "")
                throw new InvalidOperationException();

            string dataFilePath = Path.Combine(projectDirectory, projectName) + ".sqlite3";

            string newV5ProjectFilePath = projectFilePath + ".v5bak";
            string newV5DataFilePath = dataFilePath + ".v5bak";

            File.Move(projectFilePath, newV5ProjectFilePath, true);
            File.Move(dataFilePath, newV5DataFilePath, true);

            var jsonV5Library = JsonLibrary.FromFile(newV5ProjectFilePath);
            var jsonV6Library = new Model.Json.JsonLibrary(jsonV5Library);

            using (var connectionV5Data = new SqliteConnection($"Data Source={newV5DataFilePath}"))
            using (var connectionV6Data = new SqliteConnection($"Data Source={dataFilePath}"))
            {
                    connectionV5Data.Open();
                connectionV6Data.Open();

                using (var transaction = connectionV6Data.BeginTransaction())
                {
                    var createTablesCommand = connectionV6Data.CreateCommand();
                    createTablesCommand.CommandText = @"
CREATE TABLE ""Categories"" (
    ""ID"" INTEGER,
    ""String"" TEXT,
    PRIMARY KEY(""ID"" AUTOINCREMENT)
);
CREATE TABLE ""Parts"" (
    ""ID"" INTEGER,
    ""Category ID"" INTEGER,
    ""Part UID"" TEXT,
    ""Description"" TEXT,
    ""Manufacturer"" TEXT,
    ""MPN"" TEXT,
    ""Value"" TEXT,
    ""Datasheet"" TEXT,
    ""Exclude from BOM"" INTEGER,
    ""Exclude from Board"" INTEGER,
    ""Exclude from Sim"" INTEGER,
    ""Symbol Library Name"" TEXT,
    ""Symbol Name"" TEXT,
    PRIMARY KEY(""ID"" AUTOINCREMENT)
);
CREATE TABLE ""PartParameterLinks"" (
    ""Part ID"" INTEGER,
    ""Parameter Name"" TEXT,
    ""Value"" TEXT,
    PRIMARY KEY(""Part ID"", ""Parameter Name"")
);
CREATE TABLE ""PartFootprints"" (
    ""ID"" INTEGER,
    ""Part ID"" INTEGER,
    ""Library Name"" INTEGER,
    ""Name"" TEXT,
    PRIMARY KEY(""ID"" AUTOINCREMENT)
);
";
                    createTablesCommand.ExecuteNonQuery();

                    var insertCategoryCommand = connectionV6Data.CreateCommand();
                    insertCategoryCommand.CommandText = @"
INSERT INTO ""Categories"" (""ID"", ""String"")
VALUES (
    $id,
    $category_string
)
";

                    var insertCategoryCommand_IdParameter = insertCategoryCommand.CreateParameter();
                    insertCategoryCommand_IdParameter.ParameterName = "$id";
                    insertCategoryCommand.Parameters.Add(insertCategoryCommand_IdParameter);

                    var insertCategoryCommand_CategoryStringParameter = insertCategoryCommand.CreateParameter();
                    insertCategoryCommand_CategoryStringParameter.ParameterName = "$category_string";
                    insertCategoryCommand.Parameters.Add(insertCategoryCommand_CategoryStringParameter);

                    // Doesn't actually seem to affect performance, but adding for completeness
                    insertCategoryCommand.Prepare();

                    var selectCategoriesCommand = connectionV5Data.CreateCommand();
                    selectCategoriesCommand.CommandText = "SELECT * FROM \"Categories\"";
                    using (var reader = selectCategoriesCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            insertCategoryCommand_IdParameter.Value = (Int64)reader["ID"];
                            insertCategoryCommand_CategoryStringParameter.Value = (string)reader["String"];
                            insertCategoryCommand.ExecuteNonQuery();
                        }
                    }


                    var insertPartCommand = connectionV6Data.CreateCommand();
                    insertPartCommand.CommandText = @"
INSERT INTO ""Parts"" (
    ""ID"",
    ""Category ID"",
    ""Part UID"",
    ""Description"",
    ""Manufacturer"",
    ""MPN"",
    ""Value"",
    ""Datasheet"",
    ""Exclude from BOM"",
    ""Exclude from Board"",
    ""Exclude from Sim"",
    ""Symbol Library Name"",
    ""Symbol Name""
)
VALUES (
    $id,
    $category_id,
    $part_uid,
    $description,
    $manufacturer,
    $mpn,
    $value,
    $datasheet,
    $exclude_from_bom,
    $exclude_from_board,
    $exclude_from_sim,
    $symbol_lib_name,
    $symbol_name
)
";

                    var insertPartCommand_IdParameter = insertPartCommand.CreateParameter();
                    insertPartCommand_IdParameter.ParameterName = "$id";
                    insertPartCommand.Parameters.Add(insertPartCommand_IdParameter);

                    var insertPartCommand_CategoryIdParameter = insertPartCommand.CreateParameter();
                    insertPartCommand_CategoryIdParameter.ParameterName = "$category_id";
                    insertPartCommand.Parameters.Add(insertPartCommand_CategoryIdParameter);

                    var insertPartCommand_PartUIDParameter = insertPartCommand.CreateParameter();
                    insertPartCommand_PartUIDParameter.ParameterName = "$part_uid";
                    insertPartCommand.Parameters.Add(insertPartCommand_PartUIDParameter);

                    var insertPartCommand_DescriptionParameter = insertPartCommand.CreateParameter();
                    insertPartCommand_DescriptionParameter.ParameterName = "$description";
                    insertPartCommand.Parameters.Add(insertPartCommand_DescriptionParameter);

                    var insertPartCommand_ManufacturerParameter = insertPartCommand.CreateParameter();
                    insertPartCommand_ManufacturerParameter.ParameterName = "$manufacturer";
                    insertPartCommand.Parameters.Add(insertPartCommand_ManufacturerParameter);

                    var insertPartCommand_MpnParameter = insertPartCommand.CreateParameter();
                    insertPartCommand_MpnParameter.ParameterName = "$mpn";
                    insertPartCommand.Parameters.Add(insertPartCommand_MpnParameter);

                    var insertPartCommand_ValueParameter = insertPartCommand.CreateParameter();
                    insertPartCommand_ValueParameter.ParameterName = "$value";
                    insertPartCommand.Parameters.Add(insertPartCommand_ValueParameter);

                    var insertPartCommand_DatasheetParameter = insertPartCommand.CreateParameter();
                    insertPartCommand_DatasheetParameter.ParameterName = "$datasheet";
                    insertPartCommand.Parameters.Add(insertPartCommand_DatasheetParameter);

                    var insertPartCommand_ExcludeFromBomParameter = insertPartCommand.CreateParameter();
                    insertPartCommand_ExcludeFromBomParameter.ParameterName = "$exclude_from_bom";
                    insertPartCommand.Parameters.Add(insertPartCommand_ExcludeFromBomParameter);

                    var insertPartCommand_ExcludeFromBoardParameter = insertPartCommand.CreateParameter();
                    insertPartCommand_ExcludeFromBoardParameter.ParameterName = "$exclude_from_board";
                    insertPartCommand.Parameters.Add(insertPartCommand_ExcludeFromBoardParameter);

                    var insertPartCommand_ExcludeFromSimParameter = insertPartCommand.CreateParameter();
                    insertPartCommand_ExcludeFromSimParameter.ParameterName = "$exclude_from_sim";
                    insertPartCommand.Parameters.Add(insertPartCommand_ExcludeFromSimParameter);

                    var insertPartCommand_SymbolLibNameParameter = insertPartCommand.CreateParameter();
                    insertPartCommand_SymbolLibNameParameter.ParameterName = "$symbol_lib_name";
                    insertPartCommand.Parameters.Add(insertPartCommand_SymbolLibNameParameter);

                    var insertPartCommand_SymbolNameParameter = insertPartCommand.CreateParameter();
                    insertPartCommand_SymbolNameParameter.ParameterName = "$symbol_name";
                    insertPartCommand.Parameters.Add(insertPartCommand_SymbolNameParameter);

                    // Doesn't actually seem to affect performance, but adding for completeness
                    insertPartCommand.Prepare();

                    var selectPartsCommand = connectionV5Data.CreateCommand();
                    selectPartsCommand.CommandText = "SELECT * FROM \"Parts\"";
                    using (var reader = selectPartsCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            insertPartCommand_IdParameter.Value = (Int64)reader["ID"];
                            insertPartCommand_CategoryIdParameter.Value = (Int64)reader["Category ID"];
                            insertPartCommand_PartUIDParameter.Value = (string)reader["Part UID"];
                            insertPartCommand_DescriptionParameter.Value = (string)reader["Description"];
                            insertPartCommand_ManufacturerParameter.Value = (string)reader["Manufacturer"];
                            insertPartCommand_MpnParameter.Value = (string)reader["MPN"];
                            insertPartCommand_ValueParameter.Value = (string)reader["Value"];
                            insertPartCommand_DatasheetParameter.Value = (string)reader["Datasheet"];
                            insertPartCommand_ExcludeFromBomParameter.Value = (Int64)reader["Exclude from BOM"];
                            insertPartCommand_ExcludeFromBoardParameter.Value = (Int64)reader["Exclude from Board"];
                            insertPartCommand_ExcludeFromSimParameter.Value = (Int64)reader["Exclude from Sim"];
                            insertPartCommand_SymbolLibNameParameter.Value = (string)reader["Symbol Library Name"];
                            insertPartCommand_SymbolNameParameter.Value = (string)reader["Symbol Name"];
                            insertPartCommand.ExecuteNonQuery();
                        }
                    }

                    var insertPartFootprintCommand = connectionV6Data.CreateCommand();
                    insertPartFootprintCommand.CommandText = @"
INSERT INTO ""PartFootprints"" (""ID"", ""Part ID"", ""Library Name"", ""Name"")
VALUES (
    $id,
    $part_id,
    $library_name,
    $name
)
";
                    var insertPartFootprintCommand_IdParameter = insertPartFootprintCommand.CreateParameter();
                    insertPartFootprintCommand_IdParameter.ParameterName = "$id";
                    insertPartFootprintCommand.Parameters.Add(insertPartFootprintCommand_IdParameter);

                    var insertPartFootprintCommand_PartIdParameter = insertPartFootprintCommand.CreateParameter();
                    insertPartFootprintCommand_PartIdParameter.ParameterName = "$part_id";
                    insertPartFootprintCommand.Parameters.Add(insertPartFootprintCommand_PartIdParameter);

                    var insertPartFootprintCommand_LibraryNameParameter = insertPartFootprintCommand.CreateParameter();
                    insertPartFootprintCommand_LibraryNameParameter.ParameterName = "$library_name";
                    insertPartFootprintCommand.Parameters.Add(insertPartFootprintCommand_LibraryNameParameter);

                    var insertPartFootprintCommand_NameParameter = insertPartFootprintCommand.CreateParameter();
                    insertPartFootprintCommand_NameParameter.ParameterName = "$name";
                    insertPartFootprintCommand.Parameters.Add(insertPartFootprintCommand_NameParameter);

                    // Doesn't actually seem to affect performance, but adding for completeness
                    insertPartFootprintCommand.Prepare();

                    var selectPartFootprintsCommand = connectionV5Data.CreateCommand();
                    selectPartFootprintsCommand.CommandText = "SELECT * FROM \"PartFootprints\"";
                    using (var reader = selectPartFootprintsCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            insertPartFootprintCommand_IdParameter.Value = (Int64)reader["ID"];
                            insertPartFootprintCommand_PartIdParameter.Value = (Int64)reader["Part ID"];
                            insertPartFootprintCommand_LibraryNameParameter.Value = (string)reader["Library Name"];
                            insertPartFootprintCommand_NameParameter.Value = (string)reader["Name"];
                            insertPartFootprintCommand.ExecuteNonQuery();
                        }
                    }

                    // Changes actually needed on the next ones

                    var insertPartParameterLinkCommand = connectionV6Data.CreateCommand();
                    insertPartParameterLinkCommand.CommandText = @"
INSERT INTO ""PartParameterLinks"" (""Part ID"", ""Parameter Name"", ""Value"")
VALUES (
    $part_id,
    $parameter_name,
    $value
)
";

                    var insertPartParameterLinkCommand_PartIdParameter = insertPartParameterLinkCommand.CreateParameter();
                    insertPartParameterLinkCommand_PartIdParameter.ParameterName = "$part_id";
                    insertPartParameterLinkCommand.Parameters.Add(insertPartParameterLinkCommand_PartIdParameter);

                    var insertPartParameterLinkCommand_ParameterNameParameter = insertPartParameterLinkCommand.CreateParameter();
                    insertPartParameterLinkCommand_ParameterNameParameter.ParameterName = "$parameter_name";
                    insertPartParameterLinkCommand.Parameters.Add(insertPartParameterLinkCommand_ParameterNameParameter);

                    var insertPartParameterLinkCommand_ValueParameter = insertPartParameterLinkCommand.CreateParameter();
                    insertPartParameterLinkCommand_ValueParameter.ParameterName = "$value";
                    insertPartParameterLinkCommand.Parameters.Add(insertPartParameterLinkCommand_ValueParameter);

                    // Doesn't actually seem to affect performance, but adding for completeness
                    insertPartParameterLinkCommand.Prepare();

                    var selectPartParameterLinksCommand = connectionV5Data.CreateCommand();
                    selectPartsCommand.CommandText = "SELECT \"PartParameterLinks\".\"Part ID\", \"Parameters\".\"UUID\" AS \"Parameter UUID\", \"PartParameterLinks\".\"Value\" FROM \"PartParameterLinks\" INNER JOIN \"Parameters\" ON \"PartParameterLinks\".\"Parameter ID\" = \"Parameters\".\"ID\"";
                    using (var reader = selectPartsCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            insertPartParameterLinkCommand_PartIdParameter.Value = (Int64)reader["Part ID"];
                            insertPartParameterLinkCommand_ParameterNameParameter.Value = jsonV5Library.AllParameters.First(p => p.UUID.Equals((string)reader["Parameter UUID"], StringComparison.OrdinalIgnoreCase)).Name;
                            insertPartParameterLinkCommand_ValueParameter.Value = (string)reader["Value"];
                            insertPartParameterLinkCommand.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
            }
            SqliteConnection.ClearAllPools();

            jsonV6Library.WriteToFile(projectFilePath);
        }
    }
}
