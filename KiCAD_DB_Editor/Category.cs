using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using System.Xml.Linq;

namespace KiCAD_DB_Editor
{
    public class Category : NotifyObject
    {
        public static bool CheckNameValid(string name)
        {
            string trimmedNewCategoryName = name.Trim();
            return trimmedNewCategoryName != "";
        }


        // ============================================================================================
        // ============================================================================================

        private string? _name = null;
        [JsonPropertyName("name")]
        public string Name
        {
            get { Debug.Assert(_name is not null); return _name; }
            set
            {
                if (_name != value)
                {
                    if (Category.CheckNameValid(value))
                    {
                        _name = value;

                        InvokePropertyChanged();
                    }
                    else
                    {
                        throw new ArgumentException("Must not be an empty string");
                    }
                }
            }
        }

        private string? _table_name = null;
        [JsonPropertyName("table_name")]
        public string TableName
        {
            get { Debug.Assert(_table_name is not null); return _table_name; }
            set
            {
                if (_table_name != value)
                {
                    _table_name = value;

                    InvokePropertyChanged();
                }
            }
        }

        private string? _keyTableColumnName = null;
        [JsonPropertyName("key_table_column_name")]
        public string KeyTableColumnName
        {
            get { Debug.Assert(_keyTableColumnName is not null); return _keyTableColumnName; }
            set
            {
                if (_keyTableColumnName != value)
                {
                    _keyTableColumnName = value;

                    InvokePropertyChanged();
                }
            }
        }

        private string? _symbolsTableColumnName = null;
        [JsonPropertyName("symbols_table_column_name")]
        public string SymbolsTableColumnName
        {
            get { Debug.Assert(_symbolsTableColumnName is not null); return _symbolsTableColumnName; }
            set
            {
                if (_symbolsTableColumnName != value)
                {
                    _symbolsTableColumnName = value;

                    InvokePropertyChanged();
                }
            }
        }

        private string? _footprintsTableColumnName = null;
        [JsonPropertyName("footprints_table_column_name")]
        public string FootprintsTableColumnName
        {
            get { Debug.Assert(_footprintsTableColumnName is not null); return _footprintsTableColumnName; }
            set
            {
                if (_footprintsTableColumnName != value)
                {
                    _footprintsTableColumnName = value;

                    InvokePropertyChanged();
                }
            }
        }



        private ObservableCollection<SymbolFieldMap>? _symbolFieldMaps = null;
        [JsonPropertyName("symbol_field_map")]
        public ObservableCollection<SymbolFieldMap> SymbolFieldMaps
        {
            get { Debug.Assert(_symbolFieldMaps is not null); return _symbolFieldMaps; }
            set
            {
                if (_symbolFieldMaps != value)
                {
                    _symbolFieldMaps = value;

                    InvokePropertyChanged();
                }
            }
        }



        private SymbolBuiltInPropertiesMap? _symbolBuiltInPropertiesMap = null;
        [JsonPropertyName("symbol_built_in_properties_map")]
        public SymbolBuiltInPropertiesMap SymbolBuiltInPropertiesMap
        {
            get { Debug.Assert(_symbolBuiltInPropertiesMap is not null); return _symbolBuiltInPropertiesMap; }
            set
            {
                if (_symbolBuiltInPropertiesMap != value)
                {
                    _symbolBuiltInPropertiesMap = value;

                    InvokePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Exists only to get the WPF designer to believe I can use this object as DataContext
        /// </summary>
        public Category()
        {
            Name = "<Category Name>";
            TableName = "";
            KeyTableColumnName = "";
            SymbolsTableColumnName = "";
            FootprintsTableColumnName = "";
            SymbolFieldMaps = new();
            SymbolBuiltInPropertiesMap = new();
        }

        public Category(string name) : this()
        {
            Name = name;
        }

        public Category(KiCADDBL_Library kiCADDBL_Library) : this()
        {
            if (kiCADDBL_Library.Name is not null) Name = kiCADDBL_Library.Name;
            if (kiCADDBL_Library.Table is not null) TableName = kiCADDBL_Library.Table;
            if (kiCADDBL_Library.Key is not null) KeyTableColumnName = kiCADDBL_Library.Key;
            if (kiCADDBL_Library.Symbols is not null) SymbolsTableColumnName = kiCADDBL_Library.Symbols;
            if (kiCADDBL_Library.Footprints is not null) FootprintsTableColumnName = kiCADDBL_Library.Footprints;
            if (kiCADDBL_Library.Fields is not null) SymbolFieldMaps = new(kiCADDBL_Library.Fields.Select(f => new SymbolFieldMap(f)));
            if (kiCADDBL_Library.Properties is not null) SymbolBuiltInPropertiesMap = new(kiCADDBL_Library.Properties);
        }

        public override string ToString()
        {
            return $"{Name}";
        }
    }

    public class SymbolFieldMap : NotifyObject
    {
        private string? _tableColumnName = null;
        [JsonPropertyName("table_column_name")]
        public string TableColumnName
        {
            get { Debug.Assert(_tableColumnName is not null); return _tableColumnName; }
            set
            {
                if (_tableColumnName != value)
                {
                    _tableColumnName = value;

                    InvokePropertyChanged();
                }
            }
        }


        private string? _symbolFieldName = null;
        [JsonPropertyName("symbol_field_name")]
        public string SymbolFieldName
        {
            get { Debug.Assert(_symbolFieldName is not null); return _symbolFieldName; }
            set
            {
                if (_symbolFieldName != value)
                {
                    _symbolFieldName = value;

                    InvokePropertyChanged();
                }
            }
        }


        private bool? _overrideSymbolFieldVisibleOnAdd = null;
        [JsonPropertyName("override_symbol_field_visible_on_add")]
        public bool OverrideSymbolFieldVisibleOnAdd
        {
            get { Debug.Assert(_overrideSymbolFieldVisibleOnAdd is not null); return _overrideSymbolFieldVisibleOnAdd.Value; }
            set
            {
                if (_overrideSymbolFieldVisibleOnAdd != value)
                {
                    _overrideSymbolFieldVisibleOnAdd = value;

                    InvokePropertyChanged();
                }
            }
        }
        private bool? _symbolFieldVisibleOnAdd = null;
        [JsonPropertyName("symbol_field_visible_on_add")]
        public bool SymbolFieldVisibleOnAdd
        {
            get { Debug.Assert(_symbolFieldVisibleOnAdd is not null); return _symbolFieldVisibleOnAdd.Value; }
            set
            {
                if (_symbolFieldVisibleOnAdd != value)
                {
                    _symbolFieldVisibleOnAdd = value;

                    InvokePropertyChanged();
                }
            }
        }


        private bool? _overrideSymbolFieldVisibleInChooser = null;
        [JsonPropertyName("override_symbol_field_visible_in_chooser")]
        public bool OverrideSymbolFieldVisibleInChooser
        {
            get { Debug.Assert(_overrideSymbolFieldVisibleInChooser is not null); return _overrideSymbolFieldVisibleInChooser.Value; }
            set
            {
                if (_overrideSymbolFieldVisibleInChooser != value)
                {
                    _overrideSymbolFieldVisibleInChooser = value;

                    InvokePropertyChanged();
                }
            }
        }
        private bool? _symbolFieldVisibleInChooser = null;
        [JsonPropertyName("symbol_field_visible_in_chooser")]
        public bool SymbolFieldVisibleInChooser
        {
            get { Debug.Assert(_symbolFieldVisibleInChooser is not null); return _symbolFieldVisibleInChooser.Value; }
            set
            {
                if (_symbolFieldVisibleInChooser != value)
                {
                    _symbolFieldVisibleInChooser = value;

                    InvokePropertyChanged();
                }
            }
        }


        private bool? _overrideSymbolFieldShowName = null;
        [JsonPropertyName("override_symbol_field_show_name")]
        public bool OverrideSymbolFieldShowName
        {
            get { Debug.Assert(_overrideSymbolFieldShowName is not null); return _overrideSymbolFieldShowName.Value; }
            set
            {
                if (_overrideSymbolFieldShowName != value)
                {
                    _overrideSymbolFieldShowName = value;

                    InvokePropertyChanged();
                }
            }
        }
        private bool? _symbolFieldShowName = null;
        [JsonPropertyName("symbol_field_show_name")]
        public bool SymbolFieldShowName
        {
            get { Debug.Assert(_symbolFieldShowName is not null); return _symbolFieldShowName.Value; }
            set
            {
                if (_symbolFieldShowName != value)
                {
                    _symbolFieldShowName = value;

                    InvokePropertyChanged();
                }
            }
        }


        private bool? _overrideSymbolFieldInheritProperties = null;
        [JsonPropertyName("override_symbol_field_inherit_properties")]
        public bool OverrideSymbolFieldInheritProperties
        {
            get { Debug.Assert(_overrideSymbolFieldInheritProperties is not null); return _overrideSymbolFieldInheritProperties.Value; }
            set
            {
                if (_overrideSymbolFieldInheritProperties != value)
                {
                    _overrideSymbolFieldInheritProperties = value;

                    InvokePropertyChanged();
                }
            }
        }
        private bool? _symbolFieldInheritProperties = null;
        [JsonPropertyName("symbol_field_inherit_properties")]
        public bool SymbolFieldInheritProperties
        {
            get { Debug.Assert(_symbolFieldInheritProperties is not null); return _symbolFieldInheritProperties.Value; }
            set
            {
                if (_symbolFieldInheritProperties != value)
                {
                    _symbolFieldInheritProperties = value;

                    InvokePropertyChanged();
                }
            }
        }

        public SymbolFieldMap()
        {
            TableColumnName = "";
            SymbolFieldName = "";

            OverrideSymbolFieldVisibleOnAdd = false;
            SymbolFieldVisibleOnAdd = false;

            OverrideSymbolFieldVisibleInChooser = false;
            SymbolFieldVisibleInChooser = false;

            OverrideSymbolFieldShowName = false;
            SymbolFieldShowName = false;

            OverrideSymbolFieldInheritProperties = false;
            SymbolFieldInheritProperties = false;
        }

        public SymbolFieldMap(KiCADDBL_Library_Field kiCADDBL_Library_Field) : this()
        {
            if (kiCADDBL_Library_Field.Column is not null) TableColumnName = kiCADDBL_Library_Field.Column;
            if (kiCADDBL_Library_Field.Name is not null) SymbolFieldName = kiCADDBL_Library_Field.Name;

            if (kiCADDBL_Library_Field.VisibleOnAdd is not null)
            {
                OverrideSymbolFieldVisibleOnAdd = true;
                SymbolFieldVisibleOnAdd = kiCADDBL_Library_Field.VisibleOnAdd.Value;
            }
            if (kiCADDBL_Library_Field.VisibleInChooser is not null)
            {
                OverrideSymbolFieldVisibleInChooser = true;
                SymbolFieldVisibleInChooser = kiCADDBL_Library_Field.VisibleInChooser.Value;
            }
            if (kiCADDBL_Library_Field.ShowName is not null)
            {
                OverrideSymbolFieldShowName = true;
                SymbolFieldShowName = kiCADDBL_Library_Field.ShowName.Value;
            }
            if (kiCADDBL_Library_Field.InheritProperties is not null)
            {
                OverrideSymbolFieldInheritProperties = true;
                SymbolFieldInheritProperties = kiCADDBL_Library_Field.InheritProperties.Value;
            }
        }

        public override string ToString()
        {
            return $"{SymbolFieldName}";
        }
    }

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

        private bool? _symbolExcludeFromBomTableColumnName = null;
        [JsonPropertyName("symbol_exclude_from_bom_table_column_name")]
        public bool SymbolExcludeFromBomTableColumnName
        {
            get { Debug.Assert(_symbolExcludeFromBomTableColumnName is not null); return _symbolExcludeFromBomTableColumnName.Value; }
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

        private bool? _symbolExcludeFromBoardTableColumnName = null;
        [JsonPropertyName("symbol_exclude_from_board_table_column_name")]
        public bool SymbolExcludeFromBoardTableColumnName
        {
            get { Debug.Assert(_symbolExcludeFromBoardTableColumnName is not null); return _symbolExcludeFromBoardTableColumnName.Value; }
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
            UseSymbolDescriptionTableColumnName = false;
            SymbolDescriptionTableColumnName = "";
            UseSymbolKeywordsTableColumnName = false;
            SymbolKeywordsTableColumnName = "";
            UseSymbolExcludeFromBomTableColumnName = false;
            SymbolExcludeFromBomTableColumnName = false;
            UseSymbolExcludeFromBoardTableColumnName = false;
            SymbolExcludeFromBoardTableColumnName = false;
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
                SymbolExcludeFromBomTableColumnName = kiCADDBL_Library_Properties.ExcludeFromBom != 0;
            }
            if (kiCADDBL_Library_Properties.ExcludeFromBoard is not null)
            {
                UseSymbolExcludeFromBoardTableColumnName = true;
                SymbolExcludeFromBoardTableColumnName = kiCADDBL_Library_Properties.ExcludeFromBoard != 0;
            }
        }
    }
}
