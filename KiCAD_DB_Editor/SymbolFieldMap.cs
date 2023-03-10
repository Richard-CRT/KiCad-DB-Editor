using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KiCAD_DB_Editor
{
    public class SymbolFieldMap : NotifyObject
    {
        public static bool CheckNameValid(string name)
        {
            string trimmedNewSymbolFieldName = name.Trim();
            return trimmedNewSymbolFieldName != "";
        }


        // ============================================================================================
        // ============================================================================================


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
                    if (SymbolFieldMap.CheckNameValid(value))
                    {
                        _symbolFieldName = value;

                        InvokePropertyChanged();
                    }
                    else
                    {
                        throw new ArgumentException("Must not be an empty string");
                    }
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
            SymbolFieldName = "<Symbol Field Name>";

            OverrideSymbolFieldVisibleOnAdd = false;
            SymbolFieldVisibleOnAdd = false;

            OverrideSymbolFieldVisibleInChooser = false;
            SymbolFieldVisibleInChooser = false;

            OverrideSymbolFieldShowName = false;
            SymbolFieldShowName = false;

            OverrideSymbolFieldInheritProperties = false;
            SymbolFieldInheritProperties = false;
        }

        public SymbolFieldMap(string symbolFieldName) : this()
        {
            SymbolFieldName = symbolFieldName;
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
}
