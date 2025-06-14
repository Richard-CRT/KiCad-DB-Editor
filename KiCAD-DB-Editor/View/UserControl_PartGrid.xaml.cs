using KiCAD_DB_Editor.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

namespace KiCAD_DB_Editor.View
{
    /// <summary>
    /// Interaction logic for UserControl_PartGrid.xaml
    /// </summary>
    public partial class UserControl_PartGrid : UserControl
    {
        #region Dependency Properties

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

        public static readonly DependencyProperty KiCADSymbolLibraryVMsProperty = DependencyProperty.Register(
            nameof(KiCADSymbolLibraryVMs),
            typeof(ObservableCollectionEx<KiCADSymbolLibraryVM>),
            typeof(UserControl_PartGrid)
            );

        public ObservableCollectionEx<KiCADSymbolLibraryVM> KiCADSymbolLibraryVMs
        {
            get => (ObservableCollectionEx<KiCADSymbolLibraryVM>)GetValue(KiCADSymbolLibraryVMsProperty);
            set => SetValue(KiCADSymbolLibraryVMsProperty, value);
        }

        public static readonly DependencyProperty KiCADFootprintLibraryVMsProperty = DependencyProperty.Register(
            nameof(KiCADFootprintLibraryVMs),
            typeof(ObservableCollectionEx<KiCADFootprintLibraryVM>),
            typeof(UserControl_PartGrid)
            );

        public ObservableCollectionEx<KiCADFootprintLibraryVM> KiCADFootprintLibraryVMs
        {
            get => (ObservableCollectionEx<KiCADFootprintLibraryVM>)GetValue(KiCADFootprintLibraryVMsProperty);
            set => SetValue(KiCADFootprintLibraryVMsProperty, value);
        }

        public static readonly DependencyProperty ParameterVMsProperty = DependencyProperty.Register(
            nameof(ParameterVMs),
            typeof(ObservableCollectionEx<ParameterVM>),
            typeof(UserControl_PartGrid),
            new PropertyMetadata(new PropertyChangedCallback(ParameterVMsPropertyChangedCallback))
            );

        public ObservableCollectionEx<ParameterVM> ParameterVMs
        {
            get => (ObservableCollectionEx<ParameterVM>)GetValue(ParameterVMsProperty);
            set => SetValue(ParameterVMsProperty, value);
        }

        private static void ParameterVMsPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UserControl_PartGrid uc_pg)
            {
                uc_pg.ParameterVMsPropertyChanged();
            }
        }

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
            {
                uc_pg.PartVMsPropertyChanged();
            }
        }

        private ObservableCollectionEx<ParameterVM>? oldParameterVMs = null;
        protected void ParameterVMsPropertyChanged()
        {
            if (oldParameterVMs is not null)
                oldParameterVMs.CollectionChanged -= ParameterVMs_CollectionChanged;
            oldParameterVMs = ParameterVMs;
            if (ParameterVMs is not null)
                ParameterVMs.CollectionChanged += ParameterVMs_CollectionChanged;

            ParameterVMs_CollectionChanged(this, new(NotifyCollectionChangedAction.Reset));
        }

        private ObservableCollectionEx<ParameterVM>? oldParameterVMsCopy = null;
        private void ParameterVMs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (oldParameterVMsCopy is not null)
            {
                foreach (ParameterVM pVM in oldParameterVMsCopy)
                    pVM.PropertyChanged -= ParameterVM_PropertyChanged;
            }
            oldParameterVMsCopy = ParameterVMs is not null ? new(ParameterVMs) : null;
            if (oldParameterVMsCopy is not null)
            {
                foreach (ParameterVM pVM in oldParameterVMsCopy)
                    pVM.PropertyChanged += ParameterVM_PropertyChanged;
            }

            redoColumns_PotentialParametersColumnChange();
        }

        private void ParameterVM_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is ParameterVM parameterVM)
            {
                if (e.PropertyName == nameof(ParameterVM.Name))
                    redoColumns_ParameterNameChange(parameterVM);
            }
        }

        private ObservableCollectionEx<PartVM>? oldPartVMs = null;
        protected void PartVMsPropertyChanged()
        {
            if (oldPartVMs is not null)
                oldPartVMs.CollectionChanged -= PartVMs_CollectionChanged;
            oldPartVMs = PartVMs;
            if (PartVMs is not null)
                PartVMs.CollectionChanged += PartVMs_CollectionChanged;

            PartVMs_CollectionChanged(this, new(NotifyCollectionChangedAction.Reset));
        }

        private ObservableCollectionEx<PartVM>? oldPartVMsCopy = null;
        private void PartVMs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (oldPartVMsCopy is not null)
            {
                foreach (PartVM pVM in oldPartVMsCopy)
                    pVM.PropertyChanged -= PartVM_PropertyChanged;
            }
            oldPartVMsCopy = PartVMs is not null ? new(PartVMs) : null;
            if (oldPartVMsCopy is not null)
            {
                foreach (PartVM pVM in oldPartVMsCopy)
                    pVM.PropertyChanged += PartVM_PropertyChanged;
            }

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
            if (e.PropertyName == nameof(PartVM.FootprintCount))
                redoColumns_PotentialFootprintColumnChange();
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

        private DataGridTemplateColumn newFootprintColumn(int footprintIndex, bool libraryColumn)
        {
            string header;
            string valueBindingTarget;
            string optionsBindingTarget;
            if (libraryColumn)
            {
                header = $"Fprt. {footprintIndex + 1} Library";
                valueBindingTarget = $"FootprintLibraryNameAccessor[{footprintIndex}]";
                optionsBindingTarget = "KiCADFootprintLibraryVMs";
            }
            else
            {
                header = $"Fprt. {footprintIndex + 1} Name";
                valueBindingTarget = $"FootprintNameAccessor[{footprintIndex}]";
                optionsBindingTarget = $"SelectedFootprintLibraryVMAccessor[{footprintIndex}].KiCADFootprintNames";
            }

            DataGridTemplateColumn dataGridTemplateColumn;
            dataGridTemplateColumn = new();
            dataGridTemplateColumn.Header = header.Replace("_", "__");
            dataGridTemplateColumn.SortMemberPath = valueBindingTarget;

            Binding valueBinding = new(valueBindingTarget);
            valueBinding.Mode = BindingMode.TwoWay;
            valueBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

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

        private void updateParameterBindings(DataGridTextColumn column, ParameterVM parameterVM)
        {
            column.Header = parameterVM.Name.Replace("_", "__");

            Binding valueBinding = new($"ParameterAccessor[{parameterVM.UUID}]");
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

        private DataGridTextColumn newParameterColumn(ParameterVM parameterVM, int index)
        {
            DataGridTextColumn dataGridTextColumn;
            dataGridTextColumn = new();
            dataGridTextColumn.Header = parameterVM.Name.Replace("_", "__");

            Binding valueBinding = new($"ParameterAccessor[{parameterVM.UUID}]");
            valueBinding.Mode = BindingMode.TwoWay;
            valueBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            dataGridTextColumn.Binding = valueBinding;

            updateParameterBindings(dataGridTextColumn, parameterVM);

            parameterVMToDataGridColumn[parameterVM] = dataGridTextColumn;
            parameterVMsThatHaveColumns.Insert(index, parameterVM);

            const int baseColumnIndexToInsertAt = 8;
            dataGrid_Main.Columns.Insert(baseColumnIndexToInsertAt + index, dataGridTextColumn);

            return dataGridTextColumn;
        }

        private void redoColumns_ParameterNameChange(ParameterVM parameterVMWithNameChange)
        {
            DataGridTextColumn columnToUpdate = parameterVMToDataGridColumn[parameterVMWithNameChange];
            columnToUpdate.Header = parameterVMWithNameChange.Name.Replace("_", "__");
        }

        private List<ParameterVM> parameterVMsThatHaveColumns = new();
        private Dictionary<ParameterVM, DataGridTextColumn> parameterVMToDataGridColumn = new();
        private void redoColumns_PotentialParametersColumnChange()
        {
            if (ParameterVMs is not null)
            {
                var parameterVMsThatNeedANewColumn = ParameterVMs.Except(parameterVMsThatHaveColumns).ToArray();
                var parameterVMsThatNeedToBeRemoved = parameterVMsThatHaveColumns.Except(ParameterVMs).ToArray();

                foreach (ParameterVM parameterVM in parameterVMsThatNeedToBeRemoved)
                {
                    DataGridTextColumn column = parameterVMToDataGridColumn[parameterVM];
                    dataGrid_Main.Columns.Remove(column);
                    parameterVMsThatHaveColumns.Remove(parameterVM);
                    parameterVMToDataGridColumn.Remove(parameterVM);
                }

                foreach (ParameterVM parameterVM in parameterVMsThatNeedANewColumn)
                {
                    int indexOfPVMToBeAddedInParentCollection = ParameterVMs.IndexOf(parameterVM);
                    int newIndex;
                    for (newIndex = 0; newIndex < parameterVMsThatHaveColumns.Count; newIndex++)
                    {
                        if (indexOfPVMToBeAddedInParentCollection < ParameterVMs.IndexOf(parameterVMsThatHaveColumns[newIndex]))
                        {
                            break;
                        }
                    }
                    newParameterColumn(parameterVM, newIndex);
                }
            }
            else
            {
                foreach (DataGridTextColumn columnToRemove in parameterVMToDataGridColumn.Values)
                    dataGrid_Main.Columns.Remove(columnToRemove);
                parameterVMsThatHaveColumns.Clear();
                parameterVMToDataGridColumn.Clear();
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
                                        object item = dataGrid_Main.Items[destY];
                                        FrameworkElement frameworkElement = column.GetCellContent(item);
                                        // Updating the data via the frameworkElement requires Mode=TwoWay && UpdateSourceTrigger=PropertyChanged
                                        if (frameworkElement is TextBlock textBlock)
                                        {
                                            textBlock.Text = sourceData[srcY][srcX];
                                            e.Handled = true;
                                        }
                                        else if (frameworkElement is ComboBox comboBox)
                                        {
                                            comboBox.Text = sourceData[srcY][srcX];
                                            e.Handled = true;
                                        }
                                        else if (frameworkElement is CheckBox checkBox)
                                        {
                                            string s = sourceData[srcY][srcX];
                                            checkBox.IsChecked = s.ToLower() == "true" || s == "1";
                                            e.Handled = true;
                                        }
                                        else if (frameworkElement is ContentPresenter contentPresenter && VisualTreeHelper.GetChildrenCount(contentPresenter) == 1 &&
                                                VisualTreeHelper.GetChild(contentPresenter, 0) is FrameworkElement frameworkElementSubsidiary
                                                )
                                        {
                                            if (frameworkElementSubsidiary is TextBlock textBlockSubsidiary)
                                            {
                                                textBlockSubsidiary.Text = sourceData[srcY][srcX];
                                                e.Handled = true;
                                            }
                                            else if (frameworkElementSubsidiary is ComboBox comboBoxSubsidiary)
                                            {
                                                comboBoxSubsidiary.Text = sourceData[srcY][srcX];
                                                e.Handled = true;
                                            }
                                            else if (frameworkElementSubsidiary is CheckBox checkBoxSubsidiary)
                                            {
                                                string s = sourceData[srcY][srcX];
                                                checkBoxSubsidiary.IsChecked = s.ToLower() == "true" || s == "1";
                                                e.Handled = true;
                                            }
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
                    foreach (var selectedCell in selectedCells)
                    {
                        DataGridColumn column = selectedCell.Column;
                        FrameworkElement frameworkElement = column.GetCellContent(selectedCell.Item);
                        if (frameworkElement is TextBlock textBlock)
                        {
                            textBlock.Text = "";
                            e.Handled = true;
                        }
                        else if (frameworkElement is ComboBox comboBox)
                        {
                            comboBox.Text = "";
                            e.Handled = true;
                        }
                        else if (frameworkElement is CheckBox checkBox)
                        {
                            checkBox.IsChecked = false;
                            e.Handled = true;
                        }
                        else if (frameworkElement is ContentPresenter contentPresenter && VisualTreeHelper.GetChildrenCount(contentPresenter) == 1 &&
                                VisualTreeHelper.GetChild(contentPresenter, 0) is FrameworkElement frameworkElementSubsidiary
                                )
                        {
                            if (frameworkElementSubsidiary is TextBlock textBlockSubsidiary)
                            {
                                textBlockSubsidiary.Text = "";
                                e.Handled = true;
                            }
                            else if (frameworkElementSubsidiary is ComboBox comboBoxSubsidiary)
                            {
                                comboBoxSubsidiary.Text = "";
                                e.Handled = true;
                            }
                            else if (frameworkElementSubsidiary is CheckBox checkBoxSubsidiary)
                            {
                                checkBoxSubsidiary.IsChecked = false;
                                e.Handled = true;
                            }
                        }
                    }
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
                            object item = dataGrid_Main.Items[rowIndex];
                            FrameworkElement frameworkElement = column.GetCellContent(item);
                            if (frameworkElement is TextBlock textBlock)
                            {
                                selectedCellCoords[coord] = textBlock.Text;
                            }
                            else if (frameworkElement is ComboBox comboBox)
                            {
                                selectedCellCoords[coord] = comboBox.Text;
                            }
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
                                {
                                    selectedCellCoords[coord] = textBlockSubsidiary.Text;
                                }
                                else if (frameworkElementSubsidiary is ComboBox comboBoxSubsidiary)
                                {
                                    selectedCellCoords[coord] = comboBoxSubsidiary.Text;
                                }
                                else if (frameworkElementSubsidiary is CheckBox checkBoxSubsidiary)
                                {
                                    if (checkBoxSubsidiary.IsChecked is null)
                                        selectedCellCoords[coord] = "";
                                    else
                                        selectedCellCoords[coord] = (bool)checkBoxSubsidiary.IsChecked ? "True" : "False";
                                }
                                else
                                {
                                    selectedCellCoords[coord] = "";
                                }
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
}
