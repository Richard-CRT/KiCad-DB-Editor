using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor
{
    public class SymbolBuiltInPropertiesMap : NotifyObject
    {
        private bool? _useSymbolDescriptionTableColumnName = null;
        [JsonPropertyName("use_description_table_column_name")]
        public bool UseSymbolDescriptionTableColumnName
        {
            get { Debug.Assert(_useSymbolDescriptionTableColumnName is not null); return _useSymbolDescriptionTableColumnName.Value; }
            set
            {
                if (_useSymbolDescriptionTableColumnName != value)
                {
                    _useSymbolDescriptionTableColumnName = value;

                    InvokePropertyChanged();
                }
            }
        }


        private string? _symbolDescriptionTableColumnName = null;
        [JsonPropertyName("symbol_description_table_column_name")]
        public string SymbolDescriptionTableColumnName
        {
            get { Debug.Assert(_symbolDescriptionTableColumnName is not null); return _symbolDescriptionTableColumnName; }
            set
            {
                if (_symbolDescriptionTableColumnName != value)
                {
                    _symbolDescriptionTableColumnName = value;

                    InvokePropertyChanged();
                }
            }
        }


        private bool? _useSymbolKeywordsTableColumnName = null;
        [JsonPropertyName("use_symbol_keywords_table_column_name")]
        public bool UseSymbolKeywordsTableColumnName
        {
            get { Debug.Assert(_useSymbolKeywordsTableColumnName is not null); return _useSymbolKeywordsTableColumnName.Value; }
            set
            {
                if (_useSymbolKeywordsTableColumnName != value)
                {
                    _useSymbolKeywordsTableColumnName = value;

                    InvokePropertyChanged();
                }
            }
        }

        private string? _symbolKeywordsTableColumnName = null;
        [JsonPropertyName("symbol_keywords_table_column_name")]
        public string SymbolKeywordsTableColumnName
        {
            get { Debug.Assert(_symbolKeywordsTableColumnName is not null); return _symbolKeywordsTableColumnName; }
            set
            {
                if (_symbolKeywordsTableColumnName != value)
                {
                    _symbolKeywordsTableColumnName = value;

                    InvokePropertyChanged();
                }
            }
        }


        private bool? _useSymbolExcludeFromBomTableColumnName = null;
        [JsonPropertyName("use_symbol_exclude_from_bom_table_column_name")]
        public bool UseSymbolExcludeFromBomTableColumnName
        {
            get { Debug.Assert(_useSymbolExcludeFromBomTableColumnName is not null); return _useSymbolExcludeFromBomTableColumnName.Value; }
            set
            {
                if (_useSymbolExcludeFromBomTableColumnName != value)
                {
                    _useSymbolExcludeFromBomTableColumnName = value;

                    InvokePropertyChanged();
                }
            }
        }

        private string? _symbolExcludeFromBomTableColumnName = null;
        [JsonPropertyName("symbol_exclude_from_bom_table_column_name")]
        public string SymbolExcludeFromBomTableColumnName
        {
            get { Debug.Assert(_symbolExcludeFromBomTableColumnName is not null); return _symbolExcludeFromBomTableColumnName; }
            set
            {
                if (_symbolExcludeFromBomTableColumnName != value)
                {
                    _symbolExcludeFromBomTableColumnName = value;

                    InvokePropertyChanged();
                }
            }
        }


        private bool? _useSymbolExcludeFromBoardTableColumnName = null;
        [JsonPropertyName("use_symbol_exclude_from_board_table_column_name")]
        public bool UseSymbolExcludeFromBoardTableColumnName
        {
            get { Debug.Assert(_useSymbolExcludeFromBoardTableColumnName is not null); return _useSymbolExcludeFromBoardTableColumnName.Value; }
            set
            {
                if (_useSymbolExcludeFromBoardTableColumnName != value)
                {
                    _useSymbolExcludeFromBoardTableColumnName = value;

                    InvokePropertyChanged();
                }
            }
        }

        private string? _symbolExcludeFromBoardTableColumnName = null;
        [JsonPropertyName("symbol_exclude_from_board_table_column_name")]
        public string SymbolExcludeFromBoardTableColumnName
        {
            get { Debug.Assert(_symbolExcludeFromBoardTableColumnName is not null); return _symbolExcludeFromBoardTableColumnName; }
            set
            {
                if (_symbolExcludeFromBoardTableColumnName != value)
                {
                    _symbolExcludeFromBoardTableColumnName = value;

                    InvokePropertyChanged();
                }
            }
        }


        public SymbolBuiltInPropertiesMap()
        {
            UseSymbolDescriptionTableColumnName = true;
            SymbolDescriptionTableColumnName = "";
            UseSymbolKeywordsTableColumnName = false;
            SymbolKeywordsTableColumnName = "";
            UseSymbolExcludeFromBomTableColumnName = false;
            SymbolExcludeFromBomTableColumnName = "";
            UseSymbolExcludeFromBoardTableColumnName = false;
            SymbolExcludeFromBoardTableColumnName = "";
        }

        public SymbolBuiltInPropertiesMap(KiCADDBL_Library_Properties kiCADDBL_Library_Properties) : this()
        {
            if (kiCADDBL_Library_Properties.Description is not null)
            {
                UseSymbolDescriptionTableColumnName = true;
                SymbolDescriptionTableColumnName = kiCADDBL_Library_Properties.Description;
            }
            if (kiCADDBL_Library_Properties.Keywords is not null)
            {
                UseSymbolKeywordsTableColumnName = true;
                SymbolKeywordsTableColumnName = kiCADDBL_Library_Properties.Keywords;
            }
            if (kiCADDBL_Library_Properties.ExcludeFromBom is not null)
            {
                UseSymbolExcludeFromBomTableColumnName = true;
                SymbolExcludeFromBomTableColumnName = kiCADDBL_Library_Properties.ExcludeFromBom;
            }
            if (kiCADDBL_Library_Properties.ExcludeFromBoard is not null)
            {
                UseSymbolExcludeFromBoardTableColumnName = true;
                SymbolExcludeFromBoardTableColumnName = kiCADDBL_Library_Properties.ExcludeFromBoard;
            }
        }
    }
}
