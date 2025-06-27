using KiCad_DB_Editor.Model;
using KiCad_DB_Editor.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;

namespace KiCad_DB_Editor.View
{
    /// <summary>
    /// Interaction logic for UserControl_PartGrid.xaml
    /// </summary>
    public partial class UserControl_PartGrid : UserControl
    {
        #region Dependency Properties

        #region DisplayPartCategory DependencyPropertyShowParameterColumns

        public static readonly DependencyProperty DisplayPartCategoryProperty = DependencyProperty.Register(
            nameof(DisplayPartCategory),
            typeof(bool),
            typeof(UserControl_PartGrid)
            );

        public bool DisplayPartCategory
        {
            get => (bool)GetValue(DisplayPartCategoryProperty);
            set => SetValue(DisplayPartCategoryProperty, value);
        }

        #endregion

        #region ShowParameterColumns DependencyPropertyShowParameterColumns

        public static readonly DependencyProperty ShowParameterColumnsProperty = DependencyProperty.Register(
            nameof(ShowParameterColumns),
            typeof(bool),
            typeof(UserControl_PartGrid),
            new PropertyMetadata(true, new PropertyChangedCallback(ShowParameterColumnsPropertyChangedCallback))
            );

        public bool ShowParameterColumns
        {
            get => (bool)GetValue(ShowParameterColumnsProperty);
            set => SetValue(ShowParameterColumnsProperty, value);
        }

        private static void ShowParameterColumnsPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UserControl_PartGrid uc_pg)
            {
                uc_pg.ShowParameterColumnsPropertyChanged();
            }
        }

        private void ShowParameterColumnsPropertyChanged()
        {
            redoColumns_PotentialParametersColumnChange();
        }

        #endregion

        #region SelectedPartVMs DependencyProperty

        public static readonly DependencyProperty SelectedPartVMsProperty = DependencyProperty.Register(
            nameof(SelectedPartVMs),
            typeof(PartVM[]),
            typeof(UserControl_PartGrid),
            new PropertyMetadata(new PropertyChangedCallback(SelectedPartVMsPropertyChangedCallback))
            );

        public PartVM[] SelectedPartVMs
        {
            get => (PartVM[])GetValue(SelectedPartVMsProperty);
            set => SetValue(SelectedPartVMsProperty, value);
        }

        private static void SelectedPartVMsPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UserControl_PartGrid uc_pg)
            {
                uc_pg.SelectedPartVMsPropertyChanged();
            }
        }

        bool externalSelectedPartVMsPropertyChanged = false;
        bool internalSelectedPartVMsPropertyChanged = false;
        protected void SelectedPartVMsPropertyChanged()
        {
            if (!internalSelectedPartVMsPropertyChanged)
            {
                externalSelectedPartVMsPropertyChanged = true;
                dataGrid_Main.SelectedItems.Clear();
                if (SelectedPartVMs is not null)
                {
                    foreach (PartVM selectedPartVM in SelectedPartVMs)
                        dataGrid_Main.SelectedItems.Add(selectedPartVM);
                }
                externalSelectedPartVMsPropertyChanged = false;
            }
        }

        #endregion

        #region KiCadSymbolLibraries DependencyProperty

        public static readonly DependencyProperty KiCadSymbolLibrariesProperty = DependencyProperty.Register(
            nameof(KiCadSymbolLibraries),
            typeof(ObservableCollectionEx<KiCadSymbolLibrary>),
            typeof(UserControl_PartGrid)
            );

        public ObservableCollectionEx<KiCadSymbolLibrary> KiCadSymbolLibraries
        {
            get => (ObservableCollectionEx<KiCadSymbolLibrary>)GetValue(KiCadSymbolLibrariesProperty);
            set => SetValue(KiCadSymbolLibrariesProperty, value);
        }

        #endregion

        #region KiCadFootprintLibraries DependencyProperty

        public static readonly DependencyProperty KiCadFootprintLibraryVMsProperty = DependencyProperty.Register(
            nameof(KiCadFootprintLibraries),
            typeof(ObservableCollectionEx<KiCadFootprintLibrary>),
            typeof(UserControl_PartGrid)
            );

        public ObservableCollectionEx<KiCadFootprintLibrary> KiCadFootprintLibraries
        {
            get => (ObservableCollectionEx<KiCadFootprintLibrary>)GetValue(KiCadFootprintLibraryVMsProperty);
            set => SetValue(KiCadFootprintLibraryVMsProperty, value);
        }

        #endregion

        #region ParameterNamesWithVarWrapping DependencyProperty

        public static readonly DependencyProperty ParameterNamesWithVarWrappingProperty = DependencyProperty.Register(
            nameof(ParameterNamesWithVarWrapping),
            typeof(ObservableCollection<string>),
            typeof(UserControl_PartGrid)
            );

        public ObservableCollection<string> ParameterNamesWithVarWrapping
        {
            get => (ObservableCollection<string>)GetValue(ParameterNamesWithVarWrappingProperty);
            set => SetValue(ParameterNamesWithVarWrappingProperty, value);
        }

        #endregion

        #region Parameters DependencyProperty

        public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
            nameof(Parameters),
            typeof(ObservableCollectionEx<Parameter>),
            typeof(UserControl_PartGrid),
            new PropertyMetadata(new PropertyChangedCallback(ParameterVMsPropertyChangedCallback))
            );

        public ObservableCollectionEx<Parameter> Parameters
        {
            get => (ObservableCollectionEx<Parameter>)GetValue(ParametersProperty);
            set => SetValue(ParametersProperty, value);
        }

        private static void ParameterVMsPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UserControl_PartGrid uc_pg)
            {
                uc_pg.ParametersPropertyChanged();
            }
        }

        private ObservableCollectionEx<Parameter>? oldParameters = null;
        protected void ParametersPropertyChanged()
        {
            if (oldParameters is not null)
                oldParameters.CollectionChanged -= Parameters_CollectionChanged;
            oldParameters = Parameters;
            if (Parameters is not null)
                Parameters.CollectionChanged += Parameters_CollectionChanged;

            Parameters_CollectionChanged(this, new(NotifyCollectionChangedAction.Reset));
        }

        private static readonly string[] specialParameterNamesWithVarWrapping = (new string[] { "Part UID", "Manufacturer", "MPN" }).Select(pN => $"${{{pN}}}").ToArray();
        private ObservableCollectionEx<Parameter>? oldParametersCopy = null;
        private void Parameters_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (oldParametersCopy is not null)
            {
                foreach (Parameter p in oldParametersCopy)
                    p.PropertyChanged -= Parameter_PropertyChanged;
            }
            oldParametersCopy = Parameters is not null ? new(Parameters) : null;
            if (oldParametersCopy is not null)
            {
                foreach (Parameter p in oldParametersCopy)
                    p.PropertyChanged += Parameter_PropertyChanged;
            }

            // Need to regenerate the list of the Parameters with variable wrapping i.e. ${...}
            // Need to update the keys of the ParameterFilterValues dict for the accessor,
            // but don't delete the values if the parameter is staying (otherwise the existing
            // filters would be deleted, which would be annoying)
            if (Parameters is null)
            {
                ParameterNamesWithVarWrapping.Clear();
                ParameterFilterValues.Clear();
            }
            else
            {
                ParameterNamesWithVarWrapping = new(specialParameterNamesWithVarWrapping.Concat(Parameters.Select(p => $"${{{p.Name}}}")));

                foreach (Parameter parameterToAdd in Parameters.Except(ParameterFilterValues.Keys))
                    ParameterFilterValues.Add(parameterToAdd, "");

                foreach (Parameter parameterToRemove in ParameterFilterValues.Keys.Except(Parameters))
                    ParameterFilterValues.Remove(parameterToRemove);
            }
            redoColumns_PotentialParametersColumnChange(e);
        }

        private void Parameter_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is Parameter parameter)
            {
                if (e.PropertyName == nameof(Parameter.Name))
                {
                    ParameterNamesWithVarWrapping = new(specialParameterNamesWithVarWrapping.Concat(Parameters.Select(p => $"${{{p.Name}}}")));
                    redoColumns_ParameterNameChange(parameter);
                }
            }
        }

        #endregion

        #region PartVMsCollectionView DependencyProperty

        public static readonly DependencyProperty PartVMsCollectionViewProperty = DependencyProperty.Register(
            nameof(PartVMsCollectionView),
            typeof(CollectionView),
            typeof(UserControl_PartGrid)
            );

        public CollectionView PartVMsCollectionView
        {
            get => (CollectionView)GetValue(PartVMsCollectionViewProperty);
            set => SetValue(PartVMsCollectionViewProperty, value);
        }

        #endregion

        #region PartVMs DependencyProperty

        public static readonly DependencyProperty PartVMsProperty = DependencyProperty.Register(
            nameof(PartVMs),
            typeof(ObservableCollectionEx<PartVM>),
            typeof(UserControl_PartGrid),
            new PropertyMetadata(new PropertyChangedCallback(PartVMsPropertyChangedCallback))
            );

        public ObservableCollectionEx<PartVM> PartVMs
        {
            get => (ObservableCollectionEx<PartVM>)GetValue(PartVMsProperty);
            set => SetValue(PartVMsProperty, value);
        }

        private static void PartVMsPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UserControl_PartGrid uc_pg)
                uc_pg.PartVMsPropertyChanged();
        }

        private ObservableCollectionEx<PartVM>? oldPartVMs = null;
        protected void PartVMsPropertyChanged()
        {
            PartVMsCollectionView = (CollectionView)CollectionViewSource.GetDefaultView(PartVMs);
            PartVMsCollectionView.Filter = OnFilterPartVMsCollectionView;

            if (oldPartVMs is not null)
                oldPartVMs.CollectionChanged -= PartVMs_CollectionChanged;
            oldPartVMs = PartVMs;
            if (PartVMs is not null)
                PartVMs.CollectionChanged += PartVMs_CollectionChanged;

            PartVMs_CollectionChanged(this, new(NotifyCollectionChangedAction.Reset));
        }

        private ObservableCollectionEx<PartVM>? oldPartVMsCopy = null;
        private Dictionary<Part, PartVM>? partToPartVMMap = null;
        private void PartVMs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (oldPartVMsCopy is not null)
            {
                foreach (PartVM pVM in oldPartVMsCopy)
                {
                    pVM.PropertyChanged -= PartVM_PropertyChanged;
                    pVM.ParameterAccessor.PropertyChanged -= PartVM_ParameterAccessor_PropertyChanged;
                    pVM.Part.PropertyChanged -= PartVM_Part_PropertyChanged;
                }
            }
            oldPartVMsCopy = PartVMs is not null ? new(PartVMs) : null;
            if (oldPartVMsCopy is not null)
            {
                foreach (PartVM pVM in oldPartVMsCopy)
                {
                    pVM.PropertyChanged += PartVM_PropertyChanged;
                    pVM.ParameterAccessor.PropertyChanged += PartVM_ParameterAccessor_PropertyChanged;
                    pVM.Part.PropertyChanged += PartVM_Part_PropertyChanged;
                }
            }

            partToPartVMMap = PartVMs?.ToDictionary(pVM => pVM.Part);

            redoColumns_PotentialFootprintColumnChange();

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (VisualTreeHelper.GetChildrenCount(dataGrid_Main) > 0 && VisualTreeHelper.GetChild(dataGrid_Main, 0) is Decorator border)
                {
                    if (border.Child is ScrollViewer scroll) scroll.ScrollToEnd();
                }
            }
        }

        private void PartVM_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(PartVM.FootprintCount):
                    redoColumns_PotentialFootprintColumnChange();
                    break;
            }
        }

        private void PartVM_ParameterAccessor_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            IEditableCollectionView itemsView = dataGrid_Main.Items;
            if (!itemsView.IsAddingNew && !itemsView.IsEditingItem)
            {
                switch (e.PropertyName)
                {
                    case "Item[]":
                        if (sender is ParameterAccessor pA)
                        {
                            // Force refresh but only of 1 item (as opposed to calling PartVMsCollectionView.Refresh() )
                            itemsView.EditItem(pA.OwnerPartVM);
                            itemsView.CommitEdit();
                        }
                        break;
                }
            }
        }

        private void PartVM_Part_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            IEditableCollectionView itemsView = dataGrid_Main.Items;
            if (!itemsView.IsAddingNew && !itemsView.IsEditingItem)
            {
                switch (e.PropertyName)
                {
                    case nameof(Part.PartUID):
                    case nameof(Part.Manufacturer):
                    case nameof(Part.MPN):
                    case nameof(Part.Value):
                    case nameof(Part.Description):
                    case nameof(Part.Datasheet):
                        if (sender is Part part && partToPartVMMap is not null)
                        {
                            // Force refresh but only of 1 item (as opposed to calling PartVMsCollectionView.Refresh() )
                            itemsView.EditItem(partToPartVMMap[part]);
                            itemsView.CommitEdit();
                        }
                        break;
                }
            }
        }

        #endregion

        #region Filter DependencyProperties

        public static readonly DependencyProperty OverallFilterProperty = DependencyProperty.Register(nameof(OverallFilter), typeof(string), typeof(UserControl_PartGrid),
            new PropertyMetadata(new PropertyChangedCallback(OverallFilterPropertyChangedCallback)));
        public string OverallFilter { get => (string)GetValue(OverallFilterProperty); set => SetValue(OverallFilterProperty, value); }
        private static void OverallFilterPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) { if (d is UserControl_PartGrid uc_pg) uc_pg.SpecialParameterFilterPropertyChanged(); }


        public static readonly DependencyProperty CategoryFilterProperty = DependencyProperty.Register(nameof(CategoryFilter), typeof(string), typeof(UserControl_PartGrid),
            new PropertyMetadata(new PropertyChangedCallback(CategoryFilterPropertyChangedCallback)));
        public string CategoryFilter { get => (string)GetValue(CategoryFilterProperty); set => SetValue(CategoryFilterProperty, value); }
        private static void CategoryFilterPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) { if (d is UserControl_PartGrid uc_pg) uc_pg.SpecialParameterFilterPropertyChanged(); }


        public static readonly DependencyProperty PartUIDFilterProperty = DependencyProperty.Register(nameof(PartUIDFilter), typeof(string), typeof(UserControl_PartGrid),
            new PropertyMetadata(new PropertyChangedCallback(PartUIDFilterPropertyChangedCallback)));
        public string PartUIDFilter { get => (string)GetValue(PartUIDFilterProperty); set => SetValue(PartUIDFilterProperty, value); }
        private static void PartUIDFilterPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) { if (d is UserControl_PartGrid uc_pg) uc_pg.SpecialParameterFilterPropertyChanged(); }


        public static readonly DependencyProperty ManufacturerFilterProperty = DependencyProperty.Register(nameof(ManufacturerFilter), typeof(string), typeof(UserControl_PartGrid),
            new PropertyMetadata(new PropertyChangedCallback(ManufacturerFilterPropertyChangedCallback)));
        public string ManufacturerFilter { get => (string)GetValue(ManufacturerFilterProperty); set => SetValue(ManufacturerFilterProperty, value); }
        private static void ManufacturerFilterPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) { if (d is UserControl_PartGrid uc_pg) uc_pg.SpecialParameterFilterPropertyChanged(); }


        public static readonly DependencyProperty MPNFilterProperty = DependencyProperty.Register(nameof(MPNFilter), typeof(string), typeof(UserControl_PartGrid),
            new PropertyMetadata(new PropertyChangedCallback(MPNFilterPropertyChangedCallback)));
        public string MPNFilter { get => (string)GetValue(MPNFilterProperty); set => SetValue(MPNFilterProperty, value); }
        private static void MPNFilterPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) { if (d is UserControl_PartGrid uc_pg) uc_pg.SpecialParameterFilterPropertyChanged(); }


        public static readonly DependencyProperty ValueFilterProperty = DependencyProperty.Register(nameof(ValueFilter), typeof(string), typeof(UserControl_PartGrid),
            new PropertyMetadata(new PropertyChangedCallback(ValueFilterPropertyChangedCallback)));
        public string ValueFilter { get => (string)GetValue(ValueFilterProperty); set => SetValue(ValueFilterProperty, value); }
        private static void ValueFilterPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) { if (d is UserControl_PartGrid uc_pg) uc_pg.SpecialParameterFilterPropertyChanged(); }


        public static readonly DependencyProperty DescriptionFilterProperty = DependencyProperty.Register(nameof(DescriptionFilter), typeof(string), typeof(UserControl_PartGrid),
            new PropertyMetadata(new PropertyChangedCallback(DescriptionFilterPropertyChangedCallback)));
        public string DescriptionFilter { get => (string)GetValue(DescriptionFilterProperty); set => SetValue(DescriptionFilterProperty, value); }
        private static void DescriptionFilterPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) { if (d is UserControl_PartGrid uc_pg) uc_pg.SpecialParameterFilterPropertyChanged(); }


        public static readonly DependencyProperty DatasheetFilterProperty = DependencyProperty.Register(nameof(DatasheetFilter), typeof(string), typeof(UserControl_PartGrid),
            new PropertyMetadata(new PropertyChangedCallback(DatasheetFilterPropertyChangedCallback)));
        public string DatasheetFilter { get => (string)GetValue(DatasheetFilterProperty); set => SetValue(DatasheetFilterProperty, value); }
        private static void DatasheetFilterPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) { if (d is UserControl_PartGrid uc_pg) uc_pg.SpecialParameterFilterPropertyChanged(); }

        protected void SpecialParameterFilterPropertyChanged()
        {
            IEditableCollectionView itemsView = dataGrid_Main.Items;
            if (itemsView.IsAddingNew) itemsView.CommitNew();
            if (itemsView.IsEditingItem) itemsView.CommitEdit();
            PartVMsCollectionView.Refresh();
        }


        public static readonly DependencyProperty ParameterFilterAccessorProperty = DependencyProperty.Register(nameof(ParameterFilterAccessor), typeof(ParameterFilterAccessor), typeof(UserControl_PartGrid));
        public ParameterFilterAccessor ParameterFilterAccessor { get => (ParameterFilterAccessor)GetValue(ParameterFilterAccessorProperty); private set => SetValue(ParameterFilterAccessorProperty, value); }

        #endregion

        #endregion

        public Dictionary<Parameter, string> ParameterFilterValues { get; set; } = new();

        bool OnFilterPartVMsCollectionView(object item)
        {
            PartVM partVM = (PartVM)item;
            Part part = partVM.Part;

            bool overallFilterMatch = string.IsNullOrEmpty(OverallFilter) ||
                partVM.Path.Contains(OverallFilter) ||
                part.PartUID.Contains(OverallFilter) ||
                part.Manufacturer.Contains(OverallFilter) ||
                part.MPN.Contains(OverallFilter) ||
                part.Value.Contains(OverallFilter) ||
                part.Description.Contains(OverallFilter) ||
                part.Datasheet.Contains(OverallFilter);

            bool specialParameterMatch = (string.IsNullOrEmpty(CategoryFilter) || partVM.Path.Contains(CategoryFilter)) &&
                (string.IsNullOrEmpty(PartUIDFilter) || part.PartUID.Contains(PartUIDFilter)) &&
                (string.IsNullOrEmpty(ManufacturerFilter) || part.Manufacturer.Contains(ManufacturerFilter)) &&
                (string.IsNullOrEmpty(MPNFilter) || part.MPN.Contains(MPNFilter)) &&
                (string.IsNullOrEmpty(ValueFilter) || part.Value.Contains(ValueFilter)) &&
                (string.IsNullOrEmpty(DescriptionFilter) || part.Description.Contains(DescriptionFilter)) &&
                (string.IsNullOrEmpty(DatasheetFilter) || part.Datasheet.Contains(DatasheetFilter));


            if (specialParameterMatch)
            {
                bool parameterMatch = true;
                foreach ((Parameter parameter, string filterValue) in ParameterFilterValues)
                {
                    string? paramVal = null;
                    if (filterValue != "" && part.ParameterValues.TryGetValue(parameter, out paramVal) && !paramVal.Contains(filterValue))
                    {
                        parameterMatch = false;
                        break;
                    }

                    if (!overallFilterMatch)
                    {
                        // Means overall filter is set but not yet satisfied
                        if ((paramVal is not null || part.ParameterValues.TryGetValue(parameter, out paramVal)) && paramVal.Contains(OverallFilter!))
                        {
                            overallFilterMatch = true;
                        }
                    }
                }
                return overallFilterMatch && parameterMatch;
            }
            else
                return false;
        }

        private DataGridTemplateColumn newFootprintColumn(int footprintIndex, bool libraryColumn)
        {
            string header;
            string valueBindingTarget;
            string optionsBindingTarget;
            if (libraryColumn)
            {
                header = $"Fprt. {footprintIndex + 1} Library";
                valueBindingTarget = $"FootprintLibraryNameAccessor[{footprintIndex}]";
                optionsBindingTarget = "KiCadFootprintLibraries";
            }
            else
            {
                header = $"Fprt. {footprintIndex + 1} Name";
                valueBindingTarget = $"FootprintNameAccessor[{footprintIndex}]";
                optionsBindingTarget = $"SelectedFootprintLibraryAccessor[{footprintIndex}].KiCadFootprintNames";
            }

            DataGridTemplateColumn dataGridTemplateColumn;
            dataGridTemplateColumn = new();
            dataGridTemplateColumn.Header = header.Replace("_", "__");
            dataGridTemplateColumn.SortMemberPath = valueBindingTarget;

            Binding valueBinding = new(valueBindingTarget);
            valueBinding.Mode = BindingMode.TwoWay;
            valueBinding.UpdateSourceTrigger = UpdateSourceTrigger.Default;

            // Like the XAML symbol example but for footprint columns
            DataTemplate cellTemplate = new();
            FrameworkElementFactory cellTemplateFrameworkElementFactory = new(typeof(TextBlock));
            cellTemplateFrameworkElementFactory.SetBinding(TextBlock.TextProperty, valueBinding);
            cellTemplate.VisualTree = cellTemplateFrameworkElementFactory;
            dataGridTemplateColumn.CellTemplate = cellTemplate;

            Binding optionsBinding = new(optionsBindingTarget);
            if (libraryColumn)
                // Note this not present for footprint names
                optionsBinding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(UserControl_PartGrid), 1); // 1 means nearest

            // Like the XAML symbol example but for footprint columns
            DataTemplate cellEditingTemplate = new();
            FrameworkElementFactory cellEditingTemplateFrameworkElementFactory = new(typeof(ComboBox));
            cellEditingTemplateFrameworkElementFactory.SetBinding(ComboBox.TextProperty, valueBinding);
            cellEditingTemplateFrameworkElementFactory.SetBinding(ComboBox.ItemsSourceProperty, optionsBinding);
            cellEditingTemplateFrameworkElementFactory.SetValue(ComboBox.IsEditableProperty, true);
            // Don't need to unhook this as the item holding the delegate is the combobox which is the short lived object
            cellEditingTemplateFrameworkElementFactory.AddHandler(ComboBox.LoadedEvent, new RoutedEventHandler(TemplateColumn_ComboBox_Loaded));
            if (libraryColumn)
                // Note this one not present for footprint names
                cellEditingTemplateFrameworkElementFactory.SetValue(ComboBox.DisplayMemberPathProperty, "Nickname");
            cellEditingTemplate.VisualTree = cellEditingTemplateFrameworkElementFactory;
            dataGridTemplateColumn.CellEditingTemplate = cellEditingTemplate;

            // Use as a baseline the style I defined in XAML
            Style defaultStyle = (Style)dataGrid_Main.FindResource(typeof(DataGridCell));
            Style cellStyle = new(typeof(DataGridCell), defaultStyle);
            DataTrigger dataTrigger = new();
            dataTrigger.Value = null;
            dataTrigger.Binding = valueBinding;
            dataTrigger.Setters.Add(new Setter(DataGridCell.IsEnabledProperty, false));
            cellStyle.Triggers.Add(dataTrigger);
            dataGridTemplateColumn.CellStyle = cellStyle;

            if (libraryColumn)
                footprintIndexToLibraryDataGridColumn[footprintIndex] = dataGridTemplateColumn;
            else
                footprintIndexToNameDataGridColumn[footprintIndex] = dataGridTemplateColumn;

            dataGrid_Main.Columns.Add(dataGridTemplateColumn);

            return dataGridTemplateColumn;
        }

        private DataGridTemplateColumn newFootprintNameColumn(int footprintIndex)
        {
            return newFootprintColumn(footprintIndex, false);
        }

        private DataGridTemplateColumn newFootprintLibraryColumn(int footprintIndex)
        {
            return newFootprintColumn(footprintIndex, true);
        }

        private void updateParameterBindings(DataGridTextColumn column, Parameter parameter)
        {
            Binding valueBinding = new($"ParameterAccessor[{parameter.UUID}]");
            valueBinding.Mode = BindingMode.TwoWay;
            valueBinding.UpdateSourceTrigger = UpdateSourceTrigger.Default;
            column.Binding = valueBinding;

            // Use as a baseline the style I defined in XAML
            Style defaultStyle = (Style)dataGrid_Main.FindResource(typeof(DataGridCell));
            Style cellStyle = new(typeof(DataGridCell), defaultStyle);
            DataTrigger dataTrigger = new();
            dataTrigger.Value = null;
            dataTrigger.Binding = valueBinding;
            dataTrigger.Setters.Add(new Setter(DataGridCell.IsEnabledProperty, false));
            cellStyle.Triggers.Add(dataTrigger);

            column.CellStyle = cellStyle;
        }

        private StackPanel generateParameterColumnHeader(Parameter parameter)
        {
            /*
            <StackPanel Orientation="Vertical">
                <TextBlock Text="Category" HorizontalAlignment="Center" />
                <TextBox Text="{Binding DataContext.CategoryFilter, Mode=TwoWay, Source={x:Reference dummyElementToGetDataContext}, UpdateSourceTrigger=PropertyChanged, ValidatesOnExceptions=True}"/>
            </StackPanel>
            */

            StackPanel stackPanel = new();
            stackPanel.Orientation = Orientation.Vertical;

            TextBlock headerTextBlock = new();
            headerTextBlock.Text = parameter.Name;
            headerTextBlock.HorizontalAlignment = HorizontalAlignment.Center;
            stackPanel.Children.Add(headerTextBlock);

            TextBox headerTextBox = new();
            Binding valueBinding = new($"DataContext.ParameterFilterAccessor[{parameter.UUID}]");
            valueBinding.Mode = BindingMode.TwoWay;
            valueBinding.Source = dummyElementToGetDataContext;
            valueBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            valueBinding.ValidatesOnExceptions = true;
            headerTextBox.SetBinding(TextBox.TextProperty, valueBinding);
            stackPanel.Children.Add(headerTextBox);

            DataGridTextColumn dataGridTextColumn = new();
            dataGridTextColumn.Header = stackPanel;

            return stackPanel;
        }

        private DataGridTextColumn newParameterColumn(Parameter parameter, int index)
        {
            StackPanel stackPanel = generateParameterColumnHeader(parameter);

            DataGridTextColumn dataGridTextColumn = new();
            dataGridTextColumn.Header = stackPanel;

            updateParameterBindings(dataGridTextColumn, parameter);

            parameterToDataGridColumn[parameter] = dataGridTextColumn;
            parametersThatHaveColumns.Insert(index, parameter);

            const int baseColumnIndexToInsertAt = 6;
            dataGrid_Main.Columns.Insert(baseColumnIndexToInsertAt + index, dataGridTextColumn);

            return dataGridTextColumn;
        }

        private void redoColumns_ParameterNameChange(Parameter parameterWithNameChange)
        {
            DataGridTextColumn columnToUpdate = parameterToDataGridColumn[parameterWithNameChange];
            StackPanel stackPanel = generateParameterColumnHeader(parameterWithNameChange);
            columnToUpdate.Header = stackPanel;
        }

        private List<Parameter> parametersThatHaveColumns = new();
        private Dictionary<Parameter, DataGridTextColumn> parameterToDataGridColumn = new();
        private void redoColumns_PotentialParametersColumnChange(NotifyCollectionChangedEventArgs? e = null)
        {
            if (Parameters is not null && ShowParameterColumns)
            {
                NotifyCollectionChangedAction action;
                if (e is not null)
                    action = e.Action;
                else
                    action = NotifyCollectionChangedAction.Reset;

                // The Add & Remove branches are only used by the Library part grid, as the Category part grids only fire PropertyChanged (i.e. Reset) for their
                // inherited & normal etc properties
                switch (action)
                {
                    case NotifyCollectionChangedAction.Add:
                        {
                            Debug.Assert(e!.NewItems is not null && e.NewItems.Count == 1);
                            Parameter parameterThatNeedsANewColumn = (e.NewItems[0] as Parameter)!;
                            int indexOfPToBeAddedInParentCollection = e.NewStartingIndex;
                            int newIndex;
                            for (newIndex = 0; newIndex < parametersThatHaveColumns.Count; newIndex++)
                                if (indexOfPToBeAddedInParentCollection < Parameters.IndexOf(parametersThatHaveColumns[newIndex]))
                                    break;
                            newParameterColumn(parameterThatNeedsANewColumn, newIndex);
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        {
                            Debug.Assert(e!.OldItems is not null && e.OldItems.Count == 1);
                            var parameterThatNeedsToBeRemoved = (e.OldItems[0] as Parameter)!;
                            DataGridTextColumn column = parameterToDataGridColumn[parameterThatNeedsToBeRemoved];
                            dataGrid_Main.Columns.Remove(column);
                            parametersThatHaveColumns.Remove(parameterThatNeedsToBeRemoved);
                            parameterToDataGridColumn.Remove(parameterThatNeedsToBeRemoved);
                        }
                        break;
                    case NotifyCollectionChangedAction.Reset:
                    default:
                        {
                            var parametersThatNeedANewColumn = Parameters.Except(parametersThatHaveColumns).ToList();
                            var parametersThatNeedToBeRemoved = parametersThatHaveColumns.Except(Parameters).ToList();

                            // Identify the ones that aren't in the right index anymore, and add them to the pile of ones requiring removal & (re)creation
                            int parametersThatHaveColumnsIndex = 0;
                            for (int sourceParameterIndex = 0; sourceParameterIndex < Parameters.Count; sourceParameterIndex++)
                            {
                                Parameter sourceParameter = Parameters[sourceParameterIndex];
                                int originalParametersThatHaveColumnsIndex = parametersThatHaveColumns.IndexOf(sourceParameter);
                                if (originalParametersThatHaveColumnsIndex != -1)
                                {
                                    if (parametersThatHaveColumnsIndex != originalParametersThatHaveColumnsIndex)
                                    {
                                        // Parameter is later in the parametersThatHaveColumns list than it is in the parametersThatHaveColumns-subset of the source list
                                        parametersThatNeedANewColumn.Add(sourceParameter);
                                        parametersThatNeedToBeRemoved.Add(sourceParameter);
                                    }
                                    parametersThatHaveColumnsIndex++;
                                }
                            }

                            foreach (Parameter parameter in parametersThatNeedToBeRemoved)
                            {
                                DataGridTextColumn column = parameterToDataGridColumn[parameter];
                                dataGrid_Main.Columns.Remove(column);
                                parametersThatHaveColumns.Remove(parameter);
                                parameterToDataGridColumn.Remove(parameter);
                            }

                            // Inserts parameter columns at the right index
                            foreach (Parameter parameter in parametersThatNeedANewColumn)
                            {
                                int indexOfPToBeAddedInParentCollection = Parameters.IndexOf(parameter);
                                int newIndex;
                                for (newIndex = 0; newIndex < parametersThatHaveColumns.Count; newIndex++)
                                    if (indexOfPToBeAddedInParentCollection < Parameters.IndexOf(parametersThatHaveColumns[newIndex]))
                                        break;
                                newParameterColumn(parameter, newIndex);
                            }
                        }
                        break;
                }
            }
            else
            {
                foreach (DataGridTextColumn columnToRemove in parameterToDataGridColumn.Values)
                    dataGrid_Main.Columns.Remove(columnToRemove);
                parametersThatHaveColumns.Clear();
                parameterToDataGridColumn.Clear();
            }
        }

        private int previousMaxFootprints = 0;
        private Dictionary<int, DataGridColumn> footprintIndexToLibraryDataGridColumn = new();
        private Dictionary<int, DataGridColumn> footprintIndexToNameDataGridColumn = new();
        private void redoColumns_PotentialFootprintColumnChange()
        {
            if (PartVMs is not null)
            {
                int maxFootprints = 0;
                foreach (PartVM partVM in PartVMs)
                    maxFootprints = Math.Max(maxFootprints, partVM.FootprintCount);

                if (maxFootprints != previousMaxFootprints)
                {
                    if (previousMaxFootprints > maxFootprints)
                    {
                        for (int footprintIndexColumnToRemove = previousMaxFootprints - 1; footprintIndexColumnToRemove >= maxFootprints; footprintIndexColumnToRemove--)
                        {
                            DataGridColumn column1 = footprintIndexToLibraryDataGridColumn[footprintIndexColumnToRemove];
                            dataGrid_Main.Columns.Remove(column1);
                            footprintIndexToLibraryDataGridColumn.Remove(footprintIndexColumnToRemove);

                            DataGridColumn column2 = footprintIndexToNameDataGridColumn[footprintIndexColumnToRemove];
                            dataGrid_Main.Columns.Remove(column2);
                            footprintIndexToNameDataGridColumn.Remove(footprintIndexColumnToRemove);
                        }
                    }
                    else if (maxFootprints > previousMaxFootprints)
                    {
                        for (int i = previousMaxFootprints; i < maxFootprints; i++)
                        {
                            newFootprintLibraryColumn(i);
                            newFootprintNameColumn(i);
                        }
                    }

                    previousMaxFootprints = maxFootprints;
                }
            }
            else
            {
                foreach (DataGridColumn columnToRemove in footprintIndexToLibraryDataGridColumn.Values)
                    dataGrid_Main.Columns.Remove(columnToRemove);
                footprintIndexToLibraryDataGridColumn.Clear();
                foreach (DataGridColumn columnToRemove in footprintIndexToNameDataGridColumn.Values)
                    dataGrid_Main.Columns.Remove(columnToRemove);
                footprintIndexToNameDataGridColumn.Clear();
            }
        }

        public UserControl_PartGrid()
        {
            InitializeComponent();

            ParameterFilterAccessor = new(this);
        }

        private void dataGrid_Main_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (sender is DataGrid dG)
            {
                if (!externalSelectedPartVMsPropertyChanged)
                {
                    internalSelectedPartVMsPropertyChanged = true;
                    SelectedPartVMs = dG.SelectedCells.DistinctBy(c => c.Item).Select(c => (PartVM)c.Item).ToArray();
                    internalSelectedPartVMsPropertyChanged = false;
                }
            }
        }

        private void writeToFrameworkElement(FrameworkElement frameworkElement, string value, KeyEventArgs e)
        {
            // Updating the data via the frameworkElement requires Mode=TwoWay && (UpdateSourceTrigger=PropertyChanged || call UpdateSource() manually )
            // Text boxes and comboboxes are configured to use UpdateSourceTrigger=Default for Text, the others are PropertyChanged
            if (frameworkElement is TextBlock textBlock)
            {
                BindingExpression? bE = textBlock.GetBindingExpression(TextBlock.TextProperty);
                if (bE is not null && bE.ParentBinding.Mode != BindingMode.OneWay)
                {
                    textBlock.Text = value;
                    bE.UpdateSource();
                    e.Handled = true;
                }
            }
            else if (frameworkElement is ComboBox comboBox)
            {
                BindingExpression? bE = comboBox.GetBindingExpression(ComboBox.TextProperty);
                if (bE is not null && bE.ParentBinding.Mode != BindingMode.OneWay)
                {
                    comboBox.Text = value;
                    comboBox.GetBindingExpression(ComboBox.TextProperty)?.UpdateSource();
                    e.Handled = true;
                }
            }
            else if (frameworkElement is CheckBox checkBox)
            {
                BindingExpression? bE = checkBox.GetBindingExpression(CheckBox.IsCheckedProperty);
                if (bE is not null && bE.ParentBinding.Mode != BindingMode.OneWay)
                {
                    checkBox.IsChecked = value.ToLower() == "true" || value == "1";
                    e.Handled = true;
                }
            }
            else if (frameworkElement is ContentPresenter contentPresenter && VisualTreeHelper.GetChildrenCount(contentPresenter) == 1 &&
                    VisualTreeHelper.GetChild(contentPresenter, 0) is FrameworkElement frameworkElementSubsidiary
                    )
            {
                if (frameworkElementSubsidiary is TextBlock textBlockSubsidiary)
                {
                    BindingExpression? bE = textBlockSubsidiary.GetBindingExpression(TextBlock.TextProperty);
                    if (bE is not null && bE.ParentBinding.Mode != BindingMode.OneWay)
                    {
                        textBlockSubsidiary.Text = value;
                        textBlockSubsidiary.GetBindingExpression(TextBlock.TextProperty)?.UpdateSource();
                        e.Handled = true;
                    }
                }
                else if (frameworkElementSubsidiary is ComboBox comboBoxSubsidiary)
                {
                    BindingExpression? bE = comboBoxSubsidiary.GetBindingExpression(ComboBox.TextProperty);
                    if (bE is not null && bE.ParentBinding.Mode != BindingMode.OneWay)
                    {
                        comboBoxSubsidiary.Text = value;
                        comboBoxSubsidiary.GetBindingExpression(ComboBox.TextProperty)?.UpdateSource();
                        e.Handled = true;
                    }
                }
                else if (frameworkElementSubsidiary is CheckBox checkBoxSubsidiary)
                {
                    BindingExpression? bE = checkBoxSubsidiary.GetBindingExpression(CheckBox.IsCheckedProperty);
                    if (bE is not null && bE.ParentBinding.Mode != BindingMode.OneWay)
                    {
                        checkBoxSubsidiary.IsChecked = value.ToLower() == "true" || value == "1";
                        e.Handled = true;
                    }
                }

            }
        }

        private void dataGrid_Main_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (Clipboard.ContainsText())
                {
                    string clipboardText = Clipboard.GetText(TextDataFormat.Text);
                    string[] lines = clipboardText.Split(Environment.NewLine);

                    // This behaviour is quite hard to reverse engineer
                    // Excel seems to ignore the last line if it's empty when coming from C# DataGrid
                    // Coming from DB Browser it doesn't, so it does something different there, but not sure what
                    if (lines.Length > 1 && lines.Last() == "")
                        lines = lines[..^1];

                    bool allLinesSameColumnCount = true;
                    int columnCount = -1;
                    List<string[]> sourceData = new();
                    foreach (string line in lines)
                    {
                        string[] lineValues = line.Split('\t');
                        if (columnCount == -1)
                            columnCount = lineValues.Length;
                        if (lineValues.Length != columnCount)
                        {
                            allLinesSameColumnCount = false;
                            break;
                        }
                        sourceData.Add(lineValues);
                    }
                    if (allLinesSameColumnCount)
                    {
                        // Check selection is continuous rectangle and matches clipboard rectangle
                        int sourceWidth = sourceData[0].Length;
                        int sourceHeight = sourceData.Count;

                        HashSet<(int, int)> selectedCellCoords = new();
                        var selectedCells = dataGrid_Main.SelectedCells;
                        foreach (var selectedCell in selectedCells)
                        {
                            DataGridColumn column = selectedCell.Column;
                            int columnIndex = column.DisplayIndex;
                            DataGridRow row = DataGridRow.GetRowContainingElement(column.GetCellContent(selectedCell.Item));
                            int rowIndex = row.GetIndex();
                            selectedCellCoords.Add((columnIndex, rowIndex));
                        }

                        IEditableCollectionView itemsView = dataGrid_Main.Items;

                        Dictionary<(FrameworkElement, int, int), (int, int)> destCoordToSrcCoord = new();
                        if (sourceHeight == 1 && sourceWidth == 1)
                            foreach ((int destX, int destY) in selectedCellCoords)
                            {
                                DataGridColumn column = dataGrid_Main.Columns[destX];
                                PartVM item = (PartVM)dataGrid_Main.Items[destY];
                                FrameworkElement fE = column.GetCellContent(item);
                                itemsView.EditItem(item); // Important to prevent updates causing Refresh while editing
                                writeToFrameworkElement(fE, sourceData[0][0], e);
                                itemsView.CommitEdit();
                            }
                        else
                        {
                            int minX = selectedCellCoords.MinBy(c => c.Item1).Item1;
                            int minY = selectedCellCoords.MinBy(c => c.Item2).Item2;
                            int maxX = selectedCellCoords.MaxBy(c => c.Item1).Item1;
                            int maxY = selectedCellCoords.MaxBy(c => c.Item2).Item2;
                            int destWidth = maxX - minX + 1;
                            int destHeight = maxY - minY + 1;
                            if (sourceWidth == destWidth && sourceHeight == destHeight)
                            {
                                int area = destWidth * destHeight;
                                if (area == selectedCellCoords.Count)
                                {
                                    for (int srcY = 0; srcY < sourceHeight; srcY++)
                                    {
                                        for (int srcX = 0; srcX < sourceWidth; srcX++)
                                        {
                                            int destX = minX + srcX;
                                            int destY = minY + srcY;

                                            DataGridColumn column = dataGrid_Main.Columns[destX];
                                            PartVM item = (PartVM)dataGrid_Main.Items[destY];
                                            FrameworkElement fE = column.GetCellContent(item);
                                            itemsView.EditItem(item); // Important to prevent updates causing Refresh while editing
                                            writeToFrameworkElement(fE, sourceData[srcY][srcX], e);
                                            itemsView.CommitEdit();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void dataGrid_Main_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && Keyboard.Modifiers == ModifierKeys.None && !editing)
            {
                var selectedCells = dataGrid_Main.SelectedCells;
                if (selectedCells.Count > 0)
                {
                    IEditableCollectionView itemsView = dataGrid_Main.Items;
                    foreach (var selectedCell in selectedCells)
                    {
                        DataGridColumn column = selectedCell.Column;
                        FrameworkElement frameworkElement = column.GetCellContent(selectedCell.Item);
                        itemsView.EditItem(selectedCell.Item); // Important to prevent updates causing Refresh while editing
                        writeToFrameworkElement(frameworkElement, "", e);
                        itemsView.CommitEdit();
                    }
                    PartVMsCollectionView.Refresh();
                }
            }
            else if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                var selectedCells = dataGrid_Main.SelectedCells;
                if (selectedCells.Count > 0)
                {
                    Dictionary<(int, int), string> selectedCellCoords = new();
                    foreach (var selectedCell in selectedCells)
                    {
                        DataGridColumn column = selectedCell.Column;
                        int columnIndex = column.DisplayIndex;
                        DataGridRow row = DataGridRow.GetRowContainingElement(column.GetCellContent(selectedCell.Item));
                        int rowIndex = row.GetIndex();
                        (int, int) coord = (columnIndex, rowIndex);
                        if (!selectedCellCoords.ContainsKey(coord))
                        {
                            PartVM item = (PartVM)dataGrid_Main.Items[rowIndex];
                            FrameworkElement frameworkElement = column.GetCellContent(item);
                            if (frameworkElement is TextBox textBox)
                                selectedCellCoords[coord] = textBox.Text;
                            else if (frameworkElement is TextBlock textBlock)
                                selectedCellCoords[coord] = textBlock.Text;
                            else if (frameworkElement is ComboBox comboBox)
                                selectedCellCoords[coord] = comboBox.Text;
                            else if (frameworkElement is CheckBox checkBox)
                            {
                                if (checkBox.IsChecked is null)
                                    selectedCellCoords[coord] = "";
                                else
                                    selectedCellCoords[coord] = (bool)checkBox.IsChecked ? "True" : "False";
                            }
                            else if (frameworkElement is ContentPresenter contentPresenter && VisualTreeHelper.GetChildrenCount(contentPresenter) == 1 &&
                                    VisualTreeHelper.GetChild(contentPresenter, 0) is FrameworkElement frameworkElementSubsidiary
                                    )
                            {
                                if (frameworkElementSubsidiary is TextBlock textBlockSubsidiary)
                                    selectedCellCoords[coord] = textBlockSubsidiary.Text;
                                else if (frameworkElementSubsidiary is ComboBox comboBoxSubsidiary)
                                    selectedCellCoords[coord] = comboBoxSubsidiary.Text;
                                else if (frameworkElementSubsidiary is CheckBox checkBoxSubsidiary)
                                {
                                    if (checkBoxSubsidiary.IsChecked is null)
                                        selectedCellCoords[coord] = "";
                                    else
                                        selectedCellCoords[coord] = (bool)checkBoxSubsidiary.IsChecked ? "True" : "False";
                                }
                                else
                                    selectedCellCoords[coord] = "";
                            }
                            else
                                selectedCellCoords[coord] = "";
                        }
                    }
                    int minX = selectedCellCoords.Keys.MinBy(c => c.Item1).Item1;
                    int minY = selectedCellCoords.Keys.MinBy(c => c.Item2).Item2;
                    int maxX = selectedCellCoords.Keys.MaxBy(c => c.Item1).Item1;
                    int maxY = selectedCellCoords.Keys.MaxBy(c => c.Item2).Item2;
                    int sourceWidth = maxX - minX + 1;
                    int sourceHeight = maxY - minY + 1;
                    string[][] data = new string[sourceHeight][];
                    for (int y = 0; y < sourceHeight; y++)
                    {
                        data[y] = new string[sourceWidth];
                        for (int x = 0; x < sourceWidth; x++)
                            data[y][x] = "";
                    }
                    foreach (((int x, int y), string value) in selectedCellCoords)
                        data[y - minY][x - minX] = value;

                    string clipboardText = string.Join(Environment.NewLine, data.Select(s => string.Join('\t', s)));
                    Clipboard.SetText(clipboardText);
                    e.Handled = true;
                }
            }
        }

        private bool editing = false;
        private void dataGrid_Main_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            editing = true;
        }

        private void dataGrid_Main_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            editing = false;
        }

        private void dataGrid_Main_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            editing = false;
        }

        private void TemplateColumn_ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                comboBox.Focus();
            }
        }
    }

    public class ParameterFilterAccessor : NotifyObject
    {
        public readonly UserControl_PartGrid OwnerUserControl_PartGrid;

        #region Notify Properties

        public string? this[string parameterUUID]
        {
            get
            {
                Parameter? parameter = OwnerUserControl_PartGrid.Parameters.FirstOrDefault(p => p!.UUID == parameterUUID, null);
                // This will get null if you remove a parameter i.e. remove a column, then edit a different column's filtering,
                // before GC happens. The binding to the old parameter UUID will still exist, and then parameter will be null
                // Instead of erroring, we direct it towards return null; until GC comes along
                if (parameter is not null && OwnerUserControl_PartGrid.ParameterFilterValues.TryGetValue(parameter, out string? val))
                    return val;
                else
                    return null;
            }
            set
            {
                if (value is not null)
                {
                    Parameter parameter = OwnerUserControl_PartGrid.Parameters.First(p => p.UUID == parameterUUID);
                    if (OwnerUserControl_PartGrid.ParameterFilterValues.TryGetValue(parameter, out string? s))
                    {
                        if (s != value)
                        {
                            OwnerUserControl_PartGrid.ParameterFilterValues[parameter] = value;
                            InvokePropertyChanged($"Item[]");

                            IEditableCollectionView itemsView = OwnerUserControl_PartGrid.dataGrid_Main.Items;
                            if (itemsView.IsAddingNew) itemsView.CommitNew();
                            if (itemsView.IsEditingItem) itemsView.CommitEdit();
                            OwnerUserControl_PartGrid.PartVMsCollectionView.Refresh();
                        }
                    }
                }
            }
        }

        #endregion Notify Properties

        public ParameterFilterAccessor(UserControl_PartGrid ownerUC_PG)
        {
            OwnerUserControl_PartGrid = ownerUC_PG;
        }
    }
}
