using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace KiCAD_DB_Editor
{
    public class Category : NotifyObject
    {

        // ============================================================================================
        // ============================================================================================

        private Library? _parentLibrary;
        [JsonInclude, JsonPropertyName("parent_library")]
        public Library? ParentLibrary
        {
            get { return _parentLibrary; }
            set
            {
                if (_parentLibrary != value)
                {
                    _parentLibrary = value;

                    InvokePropertyChanged();
                }
            }
        }

        private string? _name = null;
        [JsonPropertyName("name")]
        public string Name
        {
            get { Debug.Assert(_name is not null); return _name; }
            set
            {
                if (_name != value)
                {
                    if (value.Trim() != "")
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


        private bool? _useCategorySpecificKeyPattern = null;
        [JsonPropertyName("use_category_specific_key_pattern")]
        public bool UseCategorySpecificKeyPattern
        {
            get { Debug.Assert(_useCategorySpecificKeyPattern is not null); return _useCategorySpecificKeyPattern.Value; }
            set
            {
                if (_useCategorySpecificKeyPattern != value)
                {
                    _useCategorySpecificKeyPattern = value;

                    InvokePropertyChanged();
                }
            }
        }
        private string? _categorySpecificKeyPattern = null;
        [JsonPropertyName("category_specific_key_pattern")]
        public string CategorySpecificKeyPattern
        {
            get { Debug.Assert(_categorySpecificKeyPattern is not null); return _categorySpecificKeyPattern; }
            set
            {
                string trimmed = value.Trim();
                if (_categorySpecificKeyPattern != trimmed)
                {
                    if (trimmed.Length <= 20 && Project.s_KeyPatternRegex.IsMatch(trimmed))
                    {
                        _categorySpecificKeyPattern = trimmed;

                        InvokePropertyChanged();
                    }
                    else
                    {
                        throw new ArgumentException("Must match key pattern");
                    }
                }
            }
        }


        private SymbolFieldMap? _selectedSymbolFieldMap = null;
        [JsonIgnore]
        public SymbolFieldMap? SelectedSymbolFieldMap
        {
            get { return _selectedSymbolFieldMap; }
            set
            {
                if (_selectedSymbolFieldMap != value)
                {
                    _selectedSymbolFieldMap = value;

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
                    if (_symbolFieldMaps is not null)
                        _symbolFieldMaps.CollectionChanged -= _symbolFieldMaps_CollectionChanged;
                    _symbolFieldMaps = value;
                    _symbolFieldMaps.CollectionChanged += _symbolFieldMaps_CollectionChanged;
                    _symbolFieldMaps_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

                    InvokePropertyChanged();
                }
            }
        }

        private void _symbolFieldMaps_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
        }

        private string? _newSymbolFieldName = null;
        [JsonIgnore]
        public string NewSymbolFieldName
        {
            get { Debug.Assert(_newSymbolFieldName is not null); return _newSymbolFieldName; }
            set
            {
                if (_newSymbolFieldName != value)
                {
                    _newSymbolFieldName = value;

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



        private bool? _databaseConnectionValid = null;
        [JsonIgnore]
        public bool DatabaseConnectionValid
        {
            get { Debug.Assert(_databaseConnectionValid is not null); return _databaseConnectionValid.Value; }
            set
            {
                if (_databaseConnectionValid != value)
                {
                    _databaseConnectionValid = value;

                    InvokePropertyChanged();
                }
            }
        }

        private string? _databaseConnectionStatus = null;
        [JsonIgnore]
        public string DatabaseConnectionStatus
        {
            get { Debug.Assert(_databaseConnectionStatus is not null); return _databaseConnectionStatus; }
            set
            {
                if (_databaseConnectionStatus != value)
                {
                    _databaseConnectionStatus = value;

                    InvokePropertyChanged();
                }
            }
        }

        private DataTable? _databaseDataTable = null;
        [JsonIgnore]
        public DataTable DatabaseDataTable
        {
            get { Debug.Assert(_databaseDataTable is not null); return _databaseDataTable; }
            set
            {
                if (_databaseDataTable != value)
                {
                    if (_databaseDataTable is not null)
                    {
                        _databaseDataTable.RowChanged -= _databaseDataTable_WriteToDatabase;
                        _databaseDataTable.RowDeleted -= _databaseDataTable_WriteToDatabase;
                    }
                    _databaseDataTable = value;
                    _databaseDataTable.RowChanged += _databaseDataTable_WriteToDatabase;
                    _databaseDataTable.RowDeleted += _databaseDataTable_WriteToDatabase;

                    InvokePropertyChanged();
                }
            }
        }

        private bool _disable_databaseDataTable_WritingToDatabase = false;
        private void _databaseDataTable_WriteToDatabase(object sender, EventArgs e)
        {
            if (_disable_databaseDataTable_WritingToDatabase)
                return;
            _performDatabaseAction(write: true);
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
            UseCategorySpecificKeyPattern = false;
            CategorySpecificKeyPattern = "CMP-####";
            NewSymbolFieldName = ""; // Exists for binding to textbox so must start as not-null
            SymbolFieldMaps = new();
            SymbolBuiltInPropertiesMap = new();
            DatabaseConnectionValid = false;
            DatabaseDataTable = new();
            DatabaseConnectionStatus = "";
        }

        public Category(Library parentLibrary, string name) : this()
        {
            _parentLibrary = parentLibrary;
            Name = name;
        }

        public Category(Library parentLibrary, KiCADDBL_Library kiCADDBL_Library) : this()
        {
            _parentLibrary = parentLibrary;
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
            if (ParentLibrary is not null)
            {
                return $"{ParentLibrary} - {Name}";
            }
            else
                return $"{Name}";
        }

        public void NewSymbolFieldMap()
        {
            string newSymbolFieldName;
            if (NewSymbolFieldName != "")
                newSymbolFieldName = NewSymbolFieldName;
            else
            {
                const string newSymbolFieldNamePrefix = $"Field ";
                const string regexPattern = @$"^{newSymbolFieldNamePrefix}\d+$";

                int currentMax = SymbolFieldMaps.Where(l => Regex.IsMatch(l.SymbolFieldName, regexPattern))
                    .Select(l => int.Parse(l.SymbolFieldName.Remove(0, newSymbolFieldNamePrefix.Length)))
                    .DefaultIfEmpty()
                    .Max();

                newSymbolFieldName = $"{newSymbolFieldNamePrefix}{currentMax + 1}";
            }

            int loc = 0;
            while (loc < SymbolFieldMaps.Count && SymbolFieldMaps[loc].SymbolFieldName.CompareTo(newSymbolFieldName) < 0) loc++;
            SymbolFieldMaps.Insert(loc, new(newSymbolFieldName));
        }

        public void DeleteSymbolFieldMap(SymbolFieldMap symbolFieldMap)
        {
            int indexOfRemoval = SymbolFieldMaps.IndexOf(symbolFieldMap);
            SymbolFieldMaps.Remove(symbolFieldMap);
            if (indexOfRemoval >= SymbolFieldMaps.Count)
                SelectedSymbolFieldMap = SymbolFieldMaps.Last();
            else
                SelectedSymbolFieldMap = SymbolFieldMaps[indexOfRemoval];
        }

        public event EventHandler DataTableUpdated;
        private Exception? _performDatabaseAction(bool write = false, bool read = false)
        {
            if (!write && !read)
                return null;
            if (ParentLibrary is not null)
            {
                string connectionString;
                if (ParentLibrary.Source.ConnectionString != "")
                    connectionString = $"{ParentLibrary.Source.ConnectionString};Connection Timeout={ParentLibrary.Source.TimeOutSeconds};";
                else
                    connectionString = $"DSN={ParentLibrary.Source.DSN};Uid={ParentLibrary.Source.Username};Pwd={ParentLibrary.Source.Password};Connection Timeout={ParentLibrary.Source.TimeOutSeconds};";

                try
                {
                    using (OdbcConnection connection = new OdbcConnection(connectionString))
                    {
                        connection.Open();

                        OdbcDataAdapter dadapter = new OdbcDataAdapter();
                        dadapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;

                        OdbcCommandBuilder builder =
                            new OdbcCommandBuilder(dadapter);
                        builder.QuotePrefix = "`";
                        builder.QuoteSuffix = "`";


                        string strSql = @$"SELECT * FROM {builder.QuoteIdentifier(TableName)}";
                        dadapter.SelectCommand = new OdbcCommand(strSql, connection);

                        if (write)
                        {
                            dadapter.Update(DatabaseDataTable);
                        }
                        if (read)
                        {
                            _disable_databaseDataTable_WritingToDatabase = true;
                            DatabaseDataTable = new();
                            dadapter.Fill(DatabaseDataTable);
                            _disable_databaseDataTable_WritingToDatabase = false;
                            DataTableUpdated?.Invoke(this, EventArgs.Empty);
                        }
                        DatabaseConnectionValid = true;
                        DatabaseConnectionStatus = "Connection Successful";
                    }
                }
                catch (OdbcException ex)
                {
                    Debug.WriteLine($"Error while accessing {connectionString}");
                    DatabaseDataTable.Clear();
                    if (ex.Message.Contains("HY000"))
                        DatabaseConnectionStatus = $"Connection Failed (Table `{TableName}` not found)";
                    else if (ex.Message.Contains("IM002"))
                        DatabaseConnectionStatus = $"Connection Failed (Database `{ParentLibrary.Source.DSN}` not found)";
                    else
                        DatabaseConnectionStatus = $"Connection Failed (Unknown)";
                    DatabaseConnectionValid = false;

                    return ex;
                }
            }
            return null;
        }

        public Exception? UpdateDatabaseDataTable()
        {
            return _performDatabaseAction(read: true);
        }

        public string GetNextPrimaryKey(List<Category> failedCategories, string? keyPattern = null)
        {
            if (keyPattern is null && UseCategorySpecificKeyPattern)
                keyPattern = CategorySpecificKeyPattern;

            bool success = UpdateDatabaseDataTable() == null;

            if (!success)
                failedCategories.Add(this);

            if (DatabaseDataTable.PrimaryKey.Length != 1)
                throw new ArgumentException("This app only supports single column primary keys");
            var primaryKeyColumn = DatabaseDataTable.PrimaryKey[0];

            string newPrimaryKey;
            // Need to find the next component ID
            if (keyPattern is not null)
            {
                IEnumerable<string> primaryKeys = DatabaseDataTable.AsEnumerable().Select(r =>
                {
                    string? pK = r.Field<string>(primaryKeyColumn.ColumnName);
                    if (pK is null)
                        throw new ArgumentException("Primary keys must not be null");
                    return pK!;
                });

                newPrimaryKey = Project.s_GetNextPrimaryKey(keyPattern, primaryKeys, true);
            }
            else
            {
                // Ask library to find it for us
                Debug.Assert(ParentLibrary is not null);
                newPrimaryKey = ParentLibrary.GetNextPrimaryKey(failedCategories);
            }

            return newPrimaryKey;
        }

        public (string, List<Category>) NewDataBaseDataTableRow()
        {
            List<Category> failedCategories = new();
            string newPrimaryKey = GetNextPrimaryKey(failedCategories);
            return (newPrimaryKey, failedCategories);
        }

        public List<Category> NewDataBaseDataTableRow(string primaryKey)
        {
            if (DatabaseDataTable.PrimaryKey.Length != 1)
                throw new ArgumentException("This app only supports single column primary keys");
            var primaryKeyColumn = DatabaseDataTable.PrimaryKey[0];

            DataRow newDR = DatabaseDataTable.NewRow();

            List<Category> failedCategories = new();
            string newPrimaryKey = GetNextPrimaryKey(failedCategories);

            newDR[primaryKeyColumn.ColumnName] = newPrimaryKey;
            try
            {
                DatabaseDataTable.Rows.Add(newDR);
            }
            catch (ConstraintException ex)
            {
                Debug.WriteLine("Constraint exception when adding new row:");
                Debug.WriteLine(ex.Message);
            }

            return failedCategories;
        }
    }
}
