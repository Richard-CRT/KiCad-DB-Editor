using KiCad_DB_Editor.Model.Legacy.V5.Json;
using KiCad_DB_Editor.Model.Loaders;
using Microsoft.Data.Sqlite;
using System.Reflection.Metadata;

namespace KiCad_DB_Editor.Model.Legacy.V5
{

    public class Upgrader : IUpgrader
    {
        public static bool Upgrade(string projectFilePath)
        {
            try
            {
                var jsonV5Library = JsonLibrary.FromFile(projectFilePath);
                var jsonV6Library = new Model.Json.JsonLibrary(jsonV5Library);

                //jsonV6Library.WriteToFile(projectFilePath);

                return false;

                //library.PartUIDScheme = jsonLibrary.PartUIDScheme;
                //library.KiCadExportPartLibraryName = jsonLibrary.KiCadExportPartLibraryName;
                //library.KiCadExportPartLibraryDescription = jsonLibrary.KiCadExportPartLibraryDescription;
                //library.KiCadExportPartLibraryEnvironmentVariable = jsonLibrary.KiCadExportPartLibraryEnvironmentVariable;
                //library.KiCadExportOdbcName = jsonLibrary.KiCadExportOdbcName;
                //library.KiCadAutoExportOnSave = jsonLibrary.KiCadAutoExportOnSave;
                //library.KiCadAutoExportRelativePath = jsonLibrary.KiCadAutoExportRelativePath;
                //library.AllParameters.AddRange(jsonLibrary.AllParameters.Select(jP => new Parameter(jP)));
                //library.TopLevelCategories.AddRange(jsonLibrary.TopLevelCategories.Select(c => new Category(c, library, null)));
                //library.AllCategories.AddRange(library.TopLevelCategories);
                //for (int i = 0; i < library.AllCategories.Count; i++)
                //    library.AllCategories.AddRange(library.AllCategories[i].Categories);
                //library.KiCadSymbolLibraries.AddRange(jsonLibrary.KiCadSymbolLibraries.Select(kSL => new KiCadSymbolLibrary(kSL, library)));
                //library.KiCadFootprintLibraries.AddRange(jsonLibrary.KiCadFootprintLibraries.Select(kFL => new KiCadFootprintLibrary(kFL, library)));

                //Dictionary<string, Category> categoryStringToCategoryMap = new();
                //foreach (Category category in library.AllCategories)
                //{
                //    string path = $"/{category.Name}";
                //    var c = category;
                //    while (c.ParentCategory is not null)
                //    {
                //        path = $"/{c.ParentCategory.Name}{path}";
                //        c = c.ParentCategory;
                //    }
                //    categoryStringToCategoryMap[path] = category;
                //}
                //Dictionary<string, Parameter> parameterUuidToParameterMap = new();
                //foreach (Parameter parameter in library.AllParameters)
                //    parameterUuidToParameterMap[parameter.UUID] = parameter;

                //using (var connection = new SqliteConnection($"Data Source={dataFilePath}"))
                //{
                //    connection.Open();

                //    Dictionary<Int64, Category> categoryIdToCategory = new();
                //    Dictionary<Int64, Parameter> parameterIdToParameter = new();
                //    Dictionary<Int64, Part> partIdToPart = new();

                //    var selectCategoriesCommand = connection.CreateCommand();
                //    selectCategoriesCommand.CommandText = "SELECT * FROM \"Categories\"";
                //    using (var reader = selectCategoriesCommand.ExecuteReader())
                //    {
                //        while (reader.Read())
                //        {
                //            var categoryId = (Int64)reader["ID"];
                //            var categoryString = (string)reader["String"];
                //            categoryIdToCategory[categoryId] = categoryStringToCategoryMap[categoryString];
                //        }
                //    }

                //    var selectParametersCommand = connection.CreateCommand();
                //    selectParametersCommand.CommandText = "SELECT * FROM \"Parameters\"";
                //    using (var reader = selectParametersCommand.ExecuteReader())
                //    {
                //        while (reader.Read())
                //        {
                //            var parameterId = (Int64)reader["ID"];
                //            var parameterUuid = (string)reader["UUID"];
                //            parameterIdToParameter[parameterId] = parameterUuidToParameterMap[parameterUuid];
                //        }
                //    }

                //    var selectPartsCommand = connection.CreateCommand();
                //    selectPartsCommand.CommandText = "SELECT * FROM \"Parts\"";
                //    using (var reader = selectPartsCommand.ExecuteReader())
                //    {
                //        while (reader.Read())
                //        {
                //            var partId = (Int64)reader["ID"];
                //            var categoryId = (Int64)reader["Category ID"];
                //            var partUID = (string)reader["Part UID"];
                //            var description = (string)reader["Description"];
                //            var manufacturer = (string)reader["Manufacturer"];
                //            var mpn = (string)reader["MPN"];
                //            var value = (string)reader["Value"];
                //            var datasheet = (string)reader["Datasheet"];
                //            var excludeFromBOM = (Int64)reader["Exclude from BOM"];
                //            var excludeFromBoard = (Int64)reader["Exclude from Board"];
                //            var excludeFromSim = (Int64)reader["Exclude from Sim"];
                //            var symbolLibraryName = (string)reader["Symbol Library Name"];
                //            var symbolName = (string)reader["Symbol Name"];

                //            Category category = categoryIdToCategory[categoryId];

                //            Part part = new(partUID, library, category);
                //            part.Description = description;
                //            part.Manufacturer = manufacturer;
                //            part.MPN = mpn;
                //            part.Value = value;
                //            part.Datasheet = datasheet;
                //            part.ExcludeFromBOM = excludeFromBOM == 1;
                //            part.ExcludeFromBoard = excludeFromBoard == 1;
                //            part.ExcludeFromSim = excludeFromSim == 1;
                //            part.SymbolLibraryName = symbolLibraryName;
                //            part.SymbolName = symbolName;

                //            partIdToPart[partId] = part;
                //            library.AllParts.Add(part);
                //            category.Parts.Add(part);
                //        }
                //    }

                //    var selectPartParameterLinksCommand = connection.CreateCommand();
                //    selectPartsCommand.CommandText = "SELECT * FROM \"PartParameterLinks\"";
                //    using (var reader = selectPartsCommand.ExecuteReader())
                //    {
                //        while (reader.Read())
                //        {
                //            var partId = (Int64)reader["Part ID"];
                //            var parameterId = (Int64)reader["Parameter ID"];
                //            var value = (string)reader["Value"];

                //            var part = partIdToPart[partId];
                //            var parameter = parameterIdToParameter[parameterId];
                //            part.ParameterValues[parameter] = value;
                //        }
                //    }

                //    var selectPartFootprintsCommand = connection.CreateCommand();
                //    // Needs to be order by ID ASC as this determines which number the footprint is on the part
                //    selectPartFootprintsCommand.CommandText = "SELECT * FROM \"PartFootprints\" ORDER BY \"ID\" ASC";
                //    using (var reader = selectPartFootprintsCommand.ExecuteReader())
                //    {
                //        while (reader.Read())
                //        {
                //            var partId = (Int64)reader["Part ID"];
                //            var libraryName = (string)reader["Library Name"];
                //            var name = (string)reader["Name"];

                //            var part = partIdToPart[partId];
                //            part.FootprintPairs.Add((libraryName, name));
                //        }
                //    }
                //}
                //SqliteConnection.ClearAllPools();
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
