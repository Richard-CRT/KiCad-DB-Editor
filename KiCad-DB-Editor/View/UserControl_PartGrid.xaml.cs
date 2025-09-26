using KiCad_DB_Editor.Commands;
using KiCad_DB_Editor.Model;
using KiCad_DB_Editor.Utilities;
using KiCad_DB_Editor.View.Converters;
using KiCad_DB_Editor.ViewModel;
using Microsoft.Win32;
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
using System.Windows.Threading;
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

        #region DisplayPartCategory DependencyProperty

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

        #region ShowCADLinkColumns DependencyProperty

        public static readonly DependencyProperty ShowCADLinkColumnsProperty = DependencyProperty.Register(
            nameof(ShowCADLinkColumns),
            typeof(bool),
            typeof(UserControl_PartGrid),
            new PropertyMetadata(false)
            );

        public bool ShowCADLinkColumns
        {
            get => (bool)GetValue(ShowCADLinkColumnsProperty);
            set => SetValue(ShowCADLinkColumnsProperty, value);
        }

        #endregion

        #region ShowParameterColumns DependencyProperty

        public static readonly DependencyProperty ShowParameterColumnsProperty = DependencyProperty.Register(
            nameof(ShowParameterColumns),
            typeof(bool),
            typeof(UserControl_PartGrid),
            new PropertyMetadata(true)
            );

        public bool ShowParameterColumns
        {
            get => (bool)GetValue(ShowParameterColumnsProperty);
            set => SetValue(ShowParameterColumnsProperty, value);
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
            if (d is UserControl_PartGrid uc_pg) uc_pg.ParametersPropertyChanged();
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
                ParameterNamesWithVarWrapping.Clear();
                ParameterNamesWithVarWrapping.AddRange(specialParameterNamesWithVarWrapping.Concat(Parameters.Select(p => $"${{{p.Name}}}")));

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
                    ParameterNamesWithVarWrapping.Clear();
                    ParameterNamesWithVarWrapping.AddRange(specialParameterNamesWithVarWrapping.Concat(Parameters.Select(p => $"${{{p.Name}}}")));
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
            if (d is UserControl_PartGrid uc_pg) uc_pg.PartVMsPropertyChanged();
        }

        private ObservableCollectionEx<PartVM>? oldPartVMs = null;
        protected void PartVMsPropertyChanged()
        {
            PartVMsCollectionView = (CollectionView)CollectionViewSource.GetDefaultView(PartVMs);
            if (PartVMsCollectionView is not null)
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


        public static readonly DependencyProperty SymbolLibraryNameFilterProperty = DependencyProperty.Register(nameof(SymbolLibraryNameFilter), typeof(string), typeof(UserControl_PartGrid),
            new PropertyMetadata(new PropertyChangedCallback(SymbolLibraryNameFilterPropertyChangedCallback)));
        public string SymbolLibraryNameFilter { get => (string)GetValue(SymbolLibraryNameFilterProperty); set => SetValue(SymbolLibraryNameFilterProperty, value); }
        private static void SymbolLibraryNameFilterPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) { if (d is UserControl_PartGrid uc_pg) uc_pg.SpecialParameterFilterPropertyChanged(); }


        public static readonly DependencyProperty SymbolNameFilterProperty = DependencyProperty.Register(nameof(SymbolNameFilter), typeof(string), typeof(UserControl_PartGrid),
            new PropertyMetadata(new PropertyChangedCallback(SymbolNameFilterPropertyChangedCallback)));
        public string SymbolNameFilter { get => (string)GetValue(SymbolNameFilterProperty); set => SetValue(SymbolNameFilterProperty, value); }
        private static void SymbolNameFilterPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) { if (d is UserControl_PartGrid uc_pg) uc_pg.SpecialParameterFilterPropertyChanged(); }


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
            PartVMsCollectionViewStartFilterTimer();
        }


        public static readonly DependencyProperty ParameterFilterAccessorProperty = DependencyProperty.Register(nameof(ParameterFilterAccessor), typeof(ParameterFilterAccessor), typeof(UserControl_PartGrid));
        public ParameterFilterAccessor ParameterFilterAccessor { get => (ParameterFilterAccessor)GetValue(ParameterFilterAccessorProperty); private set => SetValue(ParameterFilterAccessorProperty, value); }

        public static readonly DependencyProperty FootprintLibraryNameFilterAccessorProperty = DependencyProperty.Register(nameof(FootprintLibraryNameFilterAccessor), typeof(FootprintLibraryNameFilterAccessor), typeof(UserControl_PartGrid));
        public FootprintLibraryNameFilterAccessor FootprintLibraryNameFilterAccessor { get => (FootprintLibraryNameFilterAccessor)GetValue(FootprintLibraryNameFilterAccessorProperty); private set => SetValue(FootprintLibraryNameFilterAccessorProperty, value); }

        public static readonly DependencyProperty FootprintNameFilterAccessorProperty = DependencyProperty.Register(nameof(FootprintNameFilterAccessor), typeof(FootprintNameFilterAccessor), typeof(UserControl_PartGrid));
        public FootprintNameFilterAccessor FootprintNameFilterAccessor { get => (FootprintNameFilterAccessor)GetValue(FootprintNameFilterAccessorProperty); private set => SetValue(FootprintNameFilterAccessorProperty, value); }

        #endregion

        #endregion

        private ObservableCollectionEx<string> _parameterNamesWithVarWrapping = new();
        public ObservableCollectionEx<string> ParameterNamesWithVarWrapping
        {
            get { return _parameterNamesWithVarWrapping; }
        }

        public Dictionary<Parameter, string> ParameterFilterValues { get; set; } = new();
        public ObservableCollectionEx<(string, string)> FootprintFilterValuePairs = new();

        private DispatcherTimer _partVMsCollectionViewFilterTimer;
        public void PartVMsCollectionViewStartFilterTimer()
        {
            // Reset the interval
            _partVMsCollectionViewFilterTimer.Stop();
            _partVMsCollectionViewFilterTimer.Start();
        }

        private void _partVMsCollectionViewFilterTimer_Tick(object? sender, EventArgs e)
        {
            _partVMsCollectionViewFilterTimer.Stop();
            IEditableCollectionView itemsView = dataGrid_Main.Items;
            if (itemsView.IsAddingNew) itemsView.CommitNew();
            if (itemsView.IsEditingItem) itemsView.CommitEdit();
            PartVMsCollectionView.Refresh();
        }

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
                part.SymbolLibraryName.Contains(OverallFilter) ||
                part.SymbolName.Contains(OverallFilter) ||
                part.Description.Contains(OverallFilter) ||
                part.Datasheet.Contains(OverallFilter);

            bool specialParameterMatch = (string.IsNullOrEmpty(CategoryFilter) || partVM.Path.Contains(CategoryFilter)) &&
                (string.IsNullOrEmpty(PartUIDFilter) || part.PartUID.Contains(PartUIDFilter)) &&
                (string.IsNullOrEmpty(ManufacturerFilter) || part.Manufacturer.Contains(ManufacturerFilter)) &&
                (string.IsNullOrEmpty(MPNFilter) || part.MPN.Contains(MPNFilter)) &&
                (string.IsNullOrEmpty(ValueFilter) || part.Value.Contains(ValueFilter)) &&
                (string.IsNullOrEmpty(SymbolLibraryNameFilter) || part.SymbolLibraryName.Contains(SymbolLibraryNameFilter)) &&
                (string.IsNullOrEmpty(SymbolNameFilter) || part.SymbolName.Contains(SymbolNameFilter)) &&
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
                        if ((paramVal is not null || part.ParameterValues.TryGetValue(parameter, out paramVal)) && paramVal.Contains(OverallFilter))
                            overallFilterMatch = true;
                    }
                }
                if (parameterMatch)
                {
                    bool footprintMatch = true;
                    for (int footprintFilterValuePairIndex = 0; footprintFilterValuePairIndex < FootprintFilterValuePairs.Count; footprintFilterValuePairIndex++)
                    {
                        (string libraryNameFilterValue, string nameFilterValue) = FootprintFilterValuePairs[footprintFilterValuePairIndex];
                        if (footprintFilterValuePairIndex < part.FootprintPairs.Count)
                        {
                            (string libraryName, string name) = part.FootprintPairs[footprintFilterValuePairIndex];

                            if ((libraryNameFilterValue != "" && !libraryName.Contains(libraryNameFilterValue)) ||
                                (nameFilterValue != "" && !name.Contains(nameFilterValue)))
                            {
                                footprintMatch = false;
                                break;
                            }

                            if (!overallFilterMatch)
                            {
                                // Means overall filter is set but not yet satisfied
                                if (libraryName.Contains(OverallFilter) || name.Contains(OverallFilter))
                                    overallFilterMatch = true;
                            }
                        }
                        else if (libraryNameFilterValue != "" || nameFilterValue != "")
                        {
                            footprintMatch = false;
                            break;
                        }
                    }
                    return overallFilterMatch && footprintMatch;
                }
                else
                    return false;
            }
            else
                return false;
        }

        const int _baseColumnIndexToInsertFootprintColumnsAt = 8;
        private DataGridTemplateColumn newFootprintColumn(int footprintIndex, bool libraryColumn)
        {
            string header;
            string headerValueBindingTarget;
            string valueBindingTarget;
            string optionsBindingTarget;
            if (libraryColumn)
            {
                header = $"Fprt. {footprintIndex + 1} Library";
                headerValueBindingTarget = $"DataContext.FootprintLibraryNameFilterAccessor[{footprintIndex}]";
                valueBindingTarget = $"FootprintLibraryNameAccessor[{footprintIndex}]";
                optionsBindingTarget = "Part.ParentLibrary.KiCadFootprintLibraries";
            }
            else
            {
                header = $"Fprt. {footprintIndex + 1} Name";
                headerValueBindingTarget = $"DataContext.FootprintNameFilterAccessor[{footprintIndex}]";
                valueBindingTarget = $"FootprintNameAccessor[{footprintIndex}]";
                optionsBindingTarget = $"SelectedFootprintLibraryAccessor[{footprintIndex}].KiCadFootprintNames";
            }

            DataGridTemplateColumn dataGridTemplateColumn;
            dataGridTemplateColumn = new();
            dataGridTemplateColumn.SortMemberPath = valueBindingTarget;

            // Like the XAML symbol example but for footprint columns
            StackPanel stackPanel = new();
            TextBlock headerTextBlock = new();
            headerTextBlock.Text = header;
            stackPanel.Children.Add(headerTextBlock);

            TextBox headerTextBox = new();
            Binding headerValueBinding = new(headerValueBindingTarget);
            headerValueBinding.Mode = BindingMode.TwoWay;
            headerValueBinding.Source = dummyElementToGetDataContext;
            headerValueBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            headerValueBinding.ValidatesOnExceptions = true;
            headerTextBox.SetBinding(TextBox.TextProperty, headerValueBinding);
            stackPanel.Children.Add(headerTextBox);
            dataGridTemplateColumn.Header = stackPanel;

            Binding valueBinding = new(valueBindingTarget);
            valueBinding.Mode = BindingMode.TwoWay;
            valueBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

            dataGridTemplateColumn.ClipboardContentBinding = valueBinding;

            // Like the XAML symbol example but for footprint columns
            Binding columnVisibilityBinding = new("DataContext.ShowCADLinkColumns")
            {
                Source = dummyElementToGetDataContext,
                Converter = (Boolean_to_Visibility_Converter)this.Resources["Boolean_to_Visibility_Converter"]
            };
            BindingOperations.SetBinding(dataGridTemplateColumn, DataGridTemplateColumn.VisibilityProperty, columnVisibilityBinding);

            // Like the XAML symbol example but for footprint columns
            Binding columnIsReadOnlyBinding = new("DataContext.ShowCADLinkColumns")
            {
                Source = dummyElementToGetDataContext,
                Converter = (Boolean_to_NotBoolean_Converter)this.Resources["Boolean_to_NotBoolean_Converter"]
            };
            BindingOperations.SetBinding(dataGridTemplateColumn, DataGridTemplateColumn.IsReadOnlyProperty, columnIsReadOnlyBinding);

            // Like the XAML symbol example but for footprint columns
            DataTemplate cellTemplate = new();
            FrameworkElementFactory textBlockFrameworkElementFactory = new(typeof(TextBlock));
            textBlockFrameworkElementFactory.SetBinding(TextBlock.TextProperty, valueBinding);

            cellTemplate.VisualTree = textBlockFrameworkElementFactory;
            dataGridTemplateColumn.CellTemplate = cellTemplate;

            Binding optionsBinding = new(optionsBindingTarget);

            Binding editingValueBinding = new(valueBindingTarget);
            editingValueBinding.Mode = BindingMode.TwoWay;
            editingValueBinding.UpdateSourceTrigger = UpdateSourceTrigger.LostFocus;

            // Like the XAML symbol example but for footprint columns
            DataTemplate cellEditingTemplate = new();
            FrameworkElementFactory cellEditingTemplateFrameworkElementFactory = new(typeof(ComboBox));
            cellEditingTemplateFrameworkElementFactory.SetBinding(ComboBox.TextProperty, editingValueBinding);
            cellEditingTemplateFrameworkElementFactory.SetBinding(ComboBox.ItemsSourceProperty, optionsBinding);
            cellEditingTemplateFrameworkElementFactory.SetValue(ComboBox.IsEditableProperty, true);
            // Don't need to unhook this as the item holding the delegate is the combobox which is the short lived object
            cellEditingTemplateFrameworkElementFactory.AddHandler(ComboBox.LoadedEvent, new RoutedEventHandler(TemplateColumn_ComboBox_Loaded));
            if (libraryColumn)
            {
                // Note this one not present for footprint names
                cellEditingTemplateFrameworkElementFactory.SetValue(ComboBox.DisplayMemberPathProperty, "Nickname");
            }
            cellEditingTemplateFrameworkElementFactory.SetValue(ComboBox.ItemStringFormatProperty, "↪ {0}");
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

            // Need to add footprintIndex * 2 because we always insert starting at the low number first
            // so in order for columns to count up left to right we need to move along
            dataGrid_Main.Columns.Insert(_baseColumnIndexToInsertFootprintColumnsAt + (footprintIndex * 2), dataGridTemplateColumn);

            return dataGridTemplateColumn;

            /*
            ParserContext context = new ParserContext();
            context.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            context.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
            byte[] byteArray = Encoding.UTF8.GetBytes("xaml here");
            DataGridTemplateColumn dataGridTemplateColumn;
            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                dataGridTemplateColumn = (DataGridTemplateColumn)XamlReader.Load(stream, context);
            }
            */
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
            valueBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
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

            Binding visibilityBinding = new("DataContext.ShowParameterColumns")
            {
                Source = dummyElementToGetDataContext,
                Converter = (Boolean_to_Visibility_Converter)this.Resources["Boolean_to_Visibility_Converter"]
            };
            BindingOperations.SetBinding(dataGridTextColumn, DataGridTextColumn.VisibilityProperty, visibilityBinding);

            Binding isReadOnlyBinding = new("DataContext.ShowParameterColumns")
            {
                Source = dummyElementToGetDataContext,
                Converter = (Boolean_to_NotBoolean_Converter)this.Resources["Boolean_to_NotBoolean_Converter"]
            };
            BindingOperations.SetBinding(dataGridTextColumn, DataGridTextColumn.IsReadOnlyProperty, isReadOnlyBinding);

            updateParameterBindings(dataGridTextColumn, parameter);

            parameterToDataGridColumn[parameter] = dataGridTextColumn;
            parametersThatHaveColumns.Insert(index, parameter);

            dataGrid_Main.Columns.Insert(_baseColumnIndexToInsertFootprintColumnsAt + (2 * _lastMaxFootprints) + index, dataGridTextColumn);

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
            if (Parameters is not null)
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
                            BindingOperations.ClearAllBindings(column); // Not sure this is necessary but can't hurt
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
                                BindingOperations.ClearAllBindings(column); // Not sure this is necessary but can't hurt
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
                {
                    dataGrid_Main.Columns.Remove(columnToRemove);
                    BindingOperations.ClearAllBindings(columnToRemove); // Not sure this is necessary but can't hurt
                }
                parametersThatHaveColumns.Clear();
                parameterToDataGridColumn.Clear();
            }
        }

        private int _lastMaxFootprints = 0;
        private Dictionary<int, DataGridColumn> footprintIndexToLibraryDataGridColumn = new();
        private Dictionary<int, DataGridColumn> footprintIndexToNameDataGridColumn = new();
        private void redoColumns_PotentialFootprintColumnChange()
        {
            if (PartVMs is not null)
            {
                int maxFootprints = 0;
                foreach (PartVM partVM in PartVMs)
                    maxFootprints = Math.Max(maxFootprints, partVM.FootprintCount);

                if (maxFootprints != _lastMaxFootprints)
                {
                    if (_lastMaxFootprints > maxFootprints)
                    {
                        for (int footprintIndexColumnToRemove = _lastMaxFootprints - 1; footprintIndexColumnToRemove >= maxFootprints; footprintIndexColumnToRemove--)
                        {
                            FootprintFilterValuePairs.RemoveAt(footprintIndexColumnToRemove);

                            DataGridColumn column1 = footprintIndexToLibraryDataGridColumn[footprintIndexColumnToRemove];
                            dataGrid_Main.Columns.Remove(column1);
                            BindingOperations.ClearAllBindings(column1); // Not sure this is necessary but can't hurt
                            footprintIndexToLibraryDataGridColumn.Remove(footprintIndexColumnToRemove);

                            DataGridColumn column2 = footprintIndexToNameDataGridColumn[footprintIndexColumnToRemove];
                            dataGrid_Main.Columns.Remove(column2);
                            BindingOperations.ClearAllBindings(column2); // Not sure this is necessary but can't hurt
                            footprintIndexToNameDataGridColumn.Remove(footprintIndexColumnToRemove);
                        }
                    }
                    else if (maxFootprints > _lastMaxFootprints)
                    {
                        for (int i = _lastMaxFootprints; i < maxFootprints; i++)
                        {
                            FootprintFilterValuePairs.Add(("", ""));

                            newFootprintNameColumn(i);
                            newFootprintLibraryColumn(i);
                        }
                    }

                    _lastMaxFootprints = maxFootprints;
                }
            }
            else
            {
                foreach (DataGridColumn columnToRemove in footprintIndexToLibraryDataGridColumn.Values)
                {
                    dataGrid_Main.Columns.Remove(columnToRemove);
                    BindingOperations.ClearAllBindings(columnToRemove); // Not sure this is necessary but can't hurt
                }
                footprintIndexToLibraryDataGridColumn.Clear();
                foreach (DataGridColumn columnToRemove in footprintIndexToNameDataGridColumn.Values)
                {
                    dataGrid_Main.Columns.Remove(columnToRemove);
                    BindingOperations.ClearAllBindings(columnToRemove); // Not sure this is necessary but can't hurt
                }
                footprintIndexToNameDataGridColumn.Clear();
                _lastMaxFootprints = 0;
            }
        }

        public UserControl_PartGrid()
        {
            InitializeComponent();

            ParameterFilterAccessor = new(this);
            FootprintNameFilterAccessor = new(this);
            FootprintLibraryNameFilterAccessor = new(this);

            OpenDatasheetFileCommand = new BasicCommand(OpenDatasheetFileCommandExecuted, OpenDatasheetFileCommandCanExecute);
            BrowseDatasheetFileCommand = new BasicCommand(BrowseDatasheetFileCommandExecuted, BrowseDatasheetFileCommandCanExecute);

            _partVMsCollectionViewFilterTimer = new();
            _partVMsCollectionViewFilterTimer.Interval = TimeSpan.FromMilliseconds(400);
            _partVMsCollectionViewFilterTimer.Tick += _partVMsCollectionViewFilterTimer_Tick;
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

        private void _editPropertyByReflection(DataGridCellInfo cell, string newValue)
        {
            DataGridColumn column = cell.Column;
            if (!column.IsReadOnly)
            {
                var bindingBase = column.ClipboardContentBinding;

                Debug.Assert(cell.Item is PartVM);
                PartVM partVM = (PartVM)cell.Item;
                Debug.Assert(bindingBase is Binding);
                var binding = (Binding)bindingBase;
                Debug.Assert(binding.Path is not null);

                string path = binding.Path.Path;
                var pathSplit = path.Split('.').ToList();

                object iterObject = partVM;
                for (int i = 0; i < pathSplit.Count - 1; i++)
                {
                    var propertyInfo = iterObject.GetType().GetProperty(pathSplit[i]);
                    iterObject = propertyInfo!.GetValue(iterObject)!;
                }
                string lastElement = pathSplit.Last();

                object[]? index;
                PropertyInfo propertyInfoToSetValueOn;
                object objectToSetValueOn;

                if (lastElement.Contains('[') && lastElement.Contains(']'))
                {
                    int openSquareIndex = path.IndexOf('[');
                    int closeSquareIndex = path.IndexOf(']');
                    string indexString = lastElement[(openSquareIndex + 1)..closeSquareIndex];
                    if (int.TryParse(indexString, out int indexInt))
                        index = [indexInt];
                    else
                        index = [indexString];
                    lastElement = lastElement[..openSquareIndex];
                    var objectWithIndexPropertyInfo = iterObject.GetType().GetProperty(lastElement)!;
                    objectToSetValueOn = objectWithIndexPropertyInfo.GetValue(iterObject)!;
                    propertyInfoToSetValueOn = objectToSetValueOn.GetType().GetProperty("Item")!;
                }
                else
                {
                    index = null;
                    objectToSetValueOn = iterObject;
                    propertyInfoToSetValueOn = objectToSetValueOn.GetType().GetProperty(lastElement)!;
                }

                IEditableCollectionView itemsView = dataGrid_Main.Items;

                if (propertyInfoToSetValueOn.PropertyType == typeof(string))
                {
                    itemsView.EditItem(partVM); // Important to prevent updates causing Refresh while editing
                    propertyInfoToSetValueOn.SetValue(objectToSetValueOn, newValue, index);
                    itemsView.CommitEdit();
                }
                else if (propertyInfoToSetValueOn.PropertyType == typeof(bool))
                {
                    string lowerNewValue = newValue.ToLower();
                    bool? newValueBool = null;
                    if (lowerNewValue == "true") newValueBool = true;
                    else if (lowerNewValue == "false") newValueBool = false;

                    if (newValueBool is not null)
                    {
                        itemsView.EditItem(partVM); // Important to prevent updates causing Refresh while editing
                        propertyInfoToSetValueOn.SetValue(objectToSetValueOn, newValueBool, index);
                        itemsView.CommitEdit();
                    }
                }
                else
                    Debug.Assert(false);
            }
        }

        private void dataGrid_Main_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                // Mark handled regardless as otherwise it bubbles to other places and behaves oddly
                e.Handled = true;
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

                        Dictionary<(int, int), DataGridCellInfo> selectedCellCoords = new();
                        var selectedCells = dataGrid_Main.SelectedCells;
                        HashSet<int> hiddenColumnIndexes = new();
                        foreach (var selectedCell in selectedCells)
                        {
                            DataGridColumn column = selectedCell.Column;
                            int columnIndex = column.DisplayIndex;
                            if (column.Visibility == Visibility.Visible)
                            {
                                int rowIndex = dataGrid_Main.Items.IndexOf(selectedCell.Item);
                                selectedCellCoords.Add((columnIndex, rowIndex), selectedCell);
                            }
                            else
                                hiddenColumnIndexes.Add(columnIndex);
                        }

                        Dictionary<(FrameworkElement, int, int), (int, int)> destCoordToSrcCoord = new();
                        if (sourceHeight == 1 && sourceWidth == 1)
                        {
                            foreach (((int destX, int destY), DataGridCellInfo selectedCell) in selectedCellCoords)
                            {
                                _editPropertyByReflection(selectedCell, sourceData[0][0]);
                            }
                        }
                        else
                        {


                            int minX = selectedCellCoords.MinBy(kvp => kvp.Key.Item1).Key.Item1;
                            int minY = selectedCellCoords.MinBy(kvp => kvp.Key.Item2).Key.Item2;
                            int maxX = selectedCellCoords.MaxBy(kvp => kvp.Key.Item1).Key.Item1;
                            int maxY = selectedCellCoords.MaxBy(kvp => kvp.Key.Item2).Key.Item2;
                            int destWidth = maxX - minX + 1 - hiddenColumnIndexes.Count;
                            int destHeight = maxY - minY + 1;
                            if (sourceWidth == destWidth && sourceHeight == destHeight)
                            {
                                int area = destWidth * destHeight;
                                if (area == selectedCellCoords.Count)
                                {
                                    // Need to do a map from srcX to destX because of hidden columns
                                    Dictionary<int, int> srcXtoDestXMap = new();
                                    int destXIter = minX;
                                    for (int srcX = 0; srcX < sourceWidth; srcX++, destXIter++)
                                    {
                                        while (hiddenColumnIndexes.Contains(destXIter))
                                            destXIter++;
                                        srcXtoDestXMap[srcX] = destXIter;
                                    }

                                    for (int srcY = 0; srcY < sourceHeight; srcY++)
                                    {
                                        for (int srcX = 0; srcX < sourceWidth; srcX++)
                                        {
                                            int destX = srcXtoDestXMap[srcX];
                                            int destY = minY + srcY;

                                            DataGridCellInfo selectedCell = selectedCellCoords[(destX, destY)];
                                            _editPropertyByReflection(selectedCell, sourceData[srcY][srcX]);
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
                        _editPropertyByReflection(selectedCell, "");
                    }
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
                if (previewedTextInput is not null)
                {
                    comboBox.Text = previewedTextInput;
                    TextBox editableTextBox = (TextBox)comboBox.Template.FindName("PART_EditableTextBox", comboBox);
                    editableTextBox.CaretIndex = previewedTextInput.Length;
                    previewedTextInput = null;
                }
            }
        }

        private void TemplateColumn_TextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.Focus();
                if (previewedTextInput is not null)
                {
                    textBox.Text = previewedTextInput;
                    textBox.CaretIndex = previewedTextInput.Length;
                    previewedTextInput = null;
                }
                else
                    textBox.SelectAll();
            }
        }

        private string? previewedTextInput = null;

        // This relies on us only ever using DataTemplateColumns that we handle the text entry on Loaded, or
        // the dataGrid_Main_PreviewTextInput will save text to be entered that never gets entered, and will
        // auto-type weird stuff when we load a DataGridTemplateColumn that we have handled
        private void dataGrid_Main_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Source is DataGrid dg && e.OriginalSource is DataGridCell cell && !cell.IsEditing && !cell.IsReadOnly)
            {
                if (cell.Column is DataGridTemplateColumn)
                {
                    dg.BeginEdit();
                    previewedTextInput = e.Text;
                }
            }
        }

        public IBasicCommand OpenDatasheetFileCommand { get; }

        private bool OpenDatasheetFileCommandCanExecute(object? parameter)
        {
            PartVM partVM = (PartVM)dataGrid_Main.CurrentItem;
            return partVM is not null && partVM.Part.Datasheet != "";
        }

        private void OpenDatasheetFileCommandExecuted(object? parameter)
        {
            PartVM partVM = (PartVM)dataGrid_Main.CurrentItem;
            string url = partVM.Part.Datasheet;
            Debug.Assert(url != "");
            try
            {
                // Best way I can come up with for checking for web URL and absolute-ing the file-based datasheet paths
                if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                else
                {
                    string absUrl = System.IO.Path.GetFullPath(System.IO.Path.Combine(partVM.Part.ParentLibrary.ProjectDirectoryPath, url));
                    Process.Start(new ProcessStartInfo { FileName = absUrl, UseShellExecute = true });
                }
            }
            catch (Win32Exception)
            {

            }
        }

        public IBasicCommand BrowseDatasheetFileCommand { get; }

        private bool BrowseDatasheetFileCommandCanExecute(object? parameter)
        {
            PartVM partVM = (PartVM)dataGrid_Main.CurrentItem;
            return partVM is not null && partVM.Part.ParentLibrary.ProjectDirectoryPath != "";
        }

        private void BrowseDatasheetFileCommandExecuted(object? parameter)
        {
            PartVM partVM = (PartVM)dataGrid_Main.CurrentItem;
            Debug.Assert(partVM.Part.ParentLibrary.ProjectDirectoryPath != "");
            OpenFileDialog openFileDialog = new();
            openFileDialog.Title = "Open Datasheet File";
            openFileDialog.Filter = "All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
                partVM.Part.Datasheet = System.IO.Path.GetRelativePath(partVM.Part.ParentLibrary.ProjectDirectoryPath, openFileDialog.FileName);
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

                            OwnerUserControl_PartGrid.PartVMsCollectionViewStartFilterTimer();
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

    public class FootprintLibraryNameFilterAccessor : NotifyObject
    {
        public readonly UserControl_PartGrid OwnerUserControl_PartGrid;

        #region Notify Properties

        public string? this[int index]
        {
            get
            {
                if (OwnerUserControl_PartGrid.FootprintFilterValuePairs.Count > index)
                    return OwnerUserControl_PartGrid.FootprintFilterValuePairs[index].Item1;
                else
                    return null;
            }
            set
            {
                if (value is not null)
                {
                    if (OwnerUserControl_PartGrid.FootprintFilterValuePairs.Count > index)
                    {
                        OwnerUserControl_PartGrid.FootprintFilterValuePairs[index] = (value, OwnerUserControl_PartGrid.FootprintFilterValuePairs[index].Item2);
                        InvokePropertyChanged($"Item[]");

                        OwnerUserControl_PartGrid.PartVMsCollectionViewStartFilterTimer();
                    }
                }
            }
        }

        #endregion Notify Properties

        public FootprintLibraryNameFilterAccessor(UserControl_PartGrid ownerUC_PG)
        {
            OwnerUserControl_PartGrid = ownerUC_PG;
        }
    }

    public class FootprintNameFilterAccessor : NotifyObject
    {
        public readonly UserControl_PartGrid OwnerUserControl_PartGrid;

        #region Notify Properties

        public string? this[int index]
        {
            get
            {
                if (OwnerUserControl_PartGrid.FootprintFilterValuePairs.Count > index)
                    return OwnerUserControl_PartGrid.FootprintFilterValuePairs[index].Item2;
                else
                    return null;
            }
            set
            {
                if (value is not null)
                {
                    if (OwnerUserControl_PartGrid.FootprintFilterValuePairs.Count > index)
                    {
                        OwnerUserControl_PartGrid.FootprintFilterValuePairs[index] = (OwnerUserControl_PartGrid.FootprintFilterValuePairs[index].Item1, value);
                        InvokePropertyChanged($"Item[]");

                        OwnerUserControl_PartGrid.PartVMsCollectionViewStartFilterTimer();
                    }
                }
            }
        }

        #endregion Notify Properties

        public FootprintNameFilterAccessor(UserControl_PartGrid ownerUC_PG)
        {
            OwnerUserControl_PartGrid = ownerUC_PG;
        }
    }

}
