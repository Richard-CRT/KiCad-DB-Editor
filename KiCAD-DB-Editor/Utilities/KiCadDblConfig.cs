using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor.Utilities
{
    public class KiCadDblLibraryData
    {
        public string PartLibName;
        public string PartLibDescription;
        public string OdbcName;
        public List<KiCadDblTableData> kiCadDblTableDatas;

        public KiCadDblLibraryData(string partLibName, string partLibDescription, string odbcName)
        {
            PartLibName = partLibName;
            PartLibDescription = partLibDescription;
            OdbcName = odbcName;
            kiCadDblTableDatas = new();
        }
    }

    public class KiCadDblTableData
    {
        public string CategoryName;
        public string TableName;
        public string DbKeyFieldName;
        public string DbSymbolsFieldName;
        public string DbFootprintsFieldName;

        public KiCadDblTableData(string categoryName, string tableName, string dbKeyFieldName, string dbSymbolsFieldName, string dbFootprintsFieldName)
        {
            CategoryName = categoryName;
            TableName = tableName;
            DbKeyFieldName = dbKeyFieldName;
            DbSymbolsFieldName = dbSymbolsFieldName;
            DbFootprintsFieldName = dbFootprintsFieldName;
        }
    }

    public static class KiCadDblConfig
    {
        private const string contentsTemplate = @"{
    ""meta"": {
        ""version"": 1.0
    },
    ""name"": ""[[PART_LIB_NAME]]"",
    ""description"": ""[[PART_LIB_DESC]]"",
    ""source"": {
        ""type"": ""odbc"",
        ""dsn"": "",
        ""username"": "",
        ""password"": "",
        ""timeout_seconds"": 2,
        ""connection_string"": ""DSN=[[DSN_NAME]];""
    },
    ""libraries"": [[[TABLES]]
    ]
}
";

        private const string tableContentsTemplate = @"
        {
            ""name"": ""[[KICAD_CATEGORY_NAME]]"",
            ""table"": ""[[TABLE_NAME]]"",
            ""key"": ""[[DB_KEY_FIELD_NAME]]"",
            ""symbols"": ""[[DB_SYMBOLS_FIELD_NAME]]"",
            ""footprints"": ""[[DB_FOOTPRINTS_FIELD_NAME]]"",
            ""fields"": [[[FIELDS]]
            ],
            ""properties"": {[[PROPERTIES]]
            }
        }";

        private const string fieldContentsTemplate = @"
                {
                    ""column"": ""[[DB_FIELD_NAME]]"",
                    ""name"": ""[[KICAD_FIELD_NAME]]"",
                    ""visible_in_chooser"": [[FIELD_VISIBLE_IN_CHOOSER]],
                    ""inherit_properties"": [[INHERIT_SYMBOL_PROPERTIES]]
                }";

        private const string property_contents_template = @"
                ""[[KICAD_PROPERTY_NAME]]"": ""[[DB_FIELD_NAME]]"" ";

        public static string Generate(KiCadDblLibraryData kiCadDblLibraryData)
        {
            string contents = contentsTemplate;
            contents = contents.Replace("[[PART_LIB_NAME]]", kiCadDblLibraryData.PartLibName);
            contents = contents.Replace("[[PART_LIB_DESC]]", kiCadDblLibraryData.PartLibDescription);
            contents = contents.Replace("[[DSN_NAME]]", kiCadDblLibraryData.OdbcName);

            string tablesReplaceContents = "";
            foreach (var kiCadDblTableData in kiCadDblLibraryData.kiCadDblTableDatas)
            {
                tablesReplaceContents = tableContentsTemplate;
                tablesReplaceContents = tablesReplaceContents.Replace("[[TABLE_NAME]]", kiCadDblTableData.TableName);
                tablesReplaceContents = tablesReplaceContents.Replace("[[KICAD_CATEGORY_NAME]]", kiCadDblTableData.CategoryName);
                tablesReplaceContents = tablesReplaceContents.Replace("[[DB_KEY_FIELD_NAME]]", kiCadDblTableData.DbKeyFieldName);
                tablesReplaceContents = tablesReplaceContents.Replace("[[DB_SYMBOLS_FIELD_NAME]]", kiCadDblTableData.DbSymbolsFieldName);
                tablesReplaceContents = tablesReplaceContents.Replace("[[DB_FOOTPRINTS_FIELD_NAME]]", kiCadDblTableData.DbFootprintsFieldName);
            }
            tablesReplaceContents = tablesReplaceContents.Trim(',');
            contents = contents.Replace("[[TABLES]]", tablesReplaceContents);
            return contents;
        }
    }
}
