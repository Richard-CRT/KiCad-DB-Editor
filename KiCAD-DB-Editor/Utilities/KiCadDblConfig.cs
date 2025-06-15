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
        public List<KiCadDblFieldData> kiCadDblFieldDatas;
        public List<KiCadDblPropertyData> kiCadDblPropertyDatas;

        public KiCadDblTableData(string categoryName, string tableName, string dbKeyFieldName, string dbSymbolsFieldName, string dbFootprintsFieldName)
        {
            CategoryName = categoryName;
            TableName = tableName;
            DbKeyFieldName = dbKeyFieldName;
            DbSymbolsFieldName = dbSymbolsFieldName;
            DbFootprintsFieldName = dbFootprintsFieldName;
            kiCadDblFieldDatas = new();
            kiCadDblPropertyDatas = new();
        }
    }

    public class KiCadDblFieldData
    {
        public string KiCadFieldName;
        public string DbFieldName;
        public bool FieldVisibleInChooser;
        public bool InheritSymbolProperties;

        public KiCadDblFieldData(string kiCadFieldName, string dbFieldName, bool fieldVisibleInChooser, bool inheritSymbolProperties)
        {
            KiCadFieldName = kiCadFieldName;
            DbFieldName = dbFieldName;
            FieldVisibleInChooser = fieldVisibleInChooser;
            InheritSymbolProperties = inheritSymbolProperties;
        }
    }

    public class KiCadDblPropertyData
    {
        public string KiCadPropertyName;
        public string DbFieldName;

        public KiCadDblPropertyData(string kiCadPropertyName, string dbFieldName)
        {
            KiCadPropertyName = kiCadPropertyName;
            DbFieldName = dbFieldName;
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

        private const string propertyContentsTemplate = @"
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
                string tableReplaceContents = tableContentsTemplate;
                tableReplaceContents = tableReplaceContents.Replace("[[TABLE_NAME]]", kiCadDblTableData.TableName);
                tableReplaceContents = tableReplaceContents.Replace("[[KICAD_CATEGORY_NAME]]", kiCadDblTableData.CategoryName);
                tableReplaceContents = tableReplaceContents.Replace("[[DB_KEY_FIELD_NAME]]", kiCadDblTableData.DbKeyFieldName);
                tableReplaceContents = tableReplaceContents.Replace("[[DB_SYMBOLS_FIELD_NAME]]", kiCadDblTableData.DbSymbolsFieldName);
                tableReplaceContents = tableReplaceContents.Replace("[[DB_FOOTPRINTS_FIELD_NAME]]", kiCadDblTableData.DbFootprintsFieldName);

                string fieldsReplaceContents = "";
                foreach (var kicadDblFieldData in kiCadDblTableData.kiCadDblFieldDatas)
                {
                    string fieldContents = fieldContentsTemplate;
                    fieldContents = fieldContents.Replace("[[DB_FIELD_NAME]]", kicadDblFieldData.DbFieldName);
                    fieldContents = fieldContents.Replace("[[KICAD_FIELD_NAME]]", kicadDblFieldData.KiCadFieldName);
                    fieldContents = fieldContents.Replace("[[FIELD_VISIBLE_IN_CHOOSER]]", kicadDblFieldData.FieldVisibleInChooser ? "true" : "false");
                    fieldContents = fieldContents.Replace("[[INHERIT_SYMBOL_PROPERTIES]]", kicadDblFieldData.InheritSymbolProperties ? "true" : "false");
                    fieldsReplaceContents += fieldContents + ",";
                }
                if (fieldsReplaceContents.Length > 0) fieldsReplaceContents = fieldsReplaceContents[..^1];
                tableReplaceContents = tableReplaceContents.Replace("[[FIELDS]]", fieldsReplaceContents);

                string propertiesReplaceContents = "";
                foreach (var kicadDblPropertyData in kiCadDblTableData.kiCadDblPropertyDatas)
                {
                    string propertyContents = propertyContentsTemplate;
                    propertyContents = propertyContents.Replace("[[KICAD_PROPERTY_NAME]]", kicadDblPropertyData.KiCadPropertyName);
                    propertyContents = propertyContents.Replace("[[DB_FIELD_NAME]]", kicadDblPropertyData.DbFieldName);

                    propertiesReplaceContents += propertyContents + ",";
                }
                if (propertiesReplaceContents.Length > 0) propertiesReplaceContents = propertiesReplaceContents[..^1];
                tableReplaceContents = tableReplaceContents.Replace("[[PROPERTIES]]", propertiesReplaceContents);

                tablesReplaceContents += tableReplaceContents + ",";
            }
            if (tablesReplaceContents.Length > 0) tablesReplaceContents = tablesReplaceContents[..^1];
            contents = contents.Replace("[[TABLES]]", tablesReplaceContents);
            return contents;
        }
    }
}
