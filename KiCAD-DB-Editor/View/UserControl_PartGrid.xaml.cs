using KiCAD_DB_Editor.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
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
            oldParameterVMsCopy = new(ParameterVMs);
            foreach (ParameterVM pVM in oldParameterVMsCopy)
                pVM.PropertyChanged += ParameterVM_PropertyChanged;

            redoColumns();
        }

        private void ParameterVM_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // TODO At some point should check that renaming the parameter doesn't actually break things
            if (e.PropertyName == nameof(ParameterVM.Name))
                redoColumns();
        }

        private ObservableCollectionEx<PartVM>? oldPartVMs = null;
        protected void PartVMsPropertyChanged()
        {
            if (oldPartVMs is not null)
                oldPartVMs.CollectionChanged -= PartVMs_CollectionChanged;
            PartVMs.CollectionChanged += PartVMs_CollectionChanged;
            oldPartVMs = PartVMs;

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
            oldPartVMsCopy = new(PartVMs);
            foreach (PartVM pVM in oldPartVMsCopy)
                pVM.PropertyChanged += PartVM_PropertyChanged;
            redoColumns();

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
            if (e.PropertyName == "ParametersChanged")
                redoColumns();
        }

        bool internalSelectedPartVMsPropertyChanged = false;
        protected void SelectedPartVMsPropertyChanged()
        {
            if (!internalSelectedPartVMsPropertyChanged)
            {
                dataGrid_Main.SelectedItems.Clear();
                foreach (PartVM selectedPartVM in SelectedPartVMs)
                    dataGrid_Main.SelectedItems.Add(selectedPartVM);
            }
        }

        #endregion

        private void redoColumns()
        {
            const int numberSpecialColumns = 11;
            while (dataGrid_Main.Columns.Count > numberSpecialColumns)
                dataGrid_Main.Columns.RemoveAt(numberSpecialColumns);
            if (ParameterVMs is not null && PartVMs is not null)
            {

                DataGridTextColumn dataGridTextColumn;
                foreach (ParameterVM parameterVM in ParameterVMs)
                {
                    dataGridTextColumn = new();
                    dataGridTextColumn.Header = parameterVM.Name.Replace("_", "__"); ;
                    Binding binding = new($"[{parameterVM.Name}]");
                    binding.Mode = BindingMode.TwoWay;
                    binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                    binding.TargetNullValue = "\x7F";
                    dataGridTextColumn.Binding = binding;
                    dataGrid_Main.Columns.Add(dataGridTextColumn);
                }
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
                internalSelectedPartVMsPropertyChanged = true;
                SelectedPartVMs = dG.SelectedCells.DistinctBy(c => c.Item).Select(c => (PartVM)c.Item).ToArray();
                internalSelectedPartVMsPropertyChanged = false;
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
                    else if (frameworkElement is ContentPresenter contentPresenter && VisualTreeHelper.GetChildrenCount(contentPresenter) == 1 &&
                            VisualTreeHelper.GetChild(contentPresenter, 0) is FrameworkElement frameworkElementSubsidiary
                            )
                    {
                        if (frameworkElementSubsidiary is TextBlock textBlockSubsidiary)
                            textBlockSubsidiary.Text = "";
                    }
                }
            }
            else if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Dictionary<(int, int), string> selectedCellCoords = new();
                var selectedCells = dataGrid_Main.SelectedCells;
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
                            selectedCellCoords[coord] = textBlock.Text;
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
    }
}
