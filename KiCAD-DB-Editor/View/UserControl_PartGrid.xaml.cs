using KiCAD_DB_Editor.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
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

namespace KiCAD_DB_Editor.View
{
    /// <summary>
    /// Interaction logic for UserControl_PartGrid.xaml
    /// </summary>
    public partial class UserControl_PartGrid : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty ParameterVMsProperty = DependencyProperty.Register(
            nameof(ParameterVMs),
            typeof(ObservableCollectionEx<ParameterVM>),
            typeof(UserControl_PartGrid),
            new PropertyMetadata(new PropertyChangedCallback(ParameterVMsPropertyChangedCallback))
            );

        private ObservableCollectionEx<ParameterVM>? oldParameterVMs = null;
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

        private ObservableCollectionEx<PartVM>? oldPartVMs = null;
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

        protected void ParameterVMsPropertyChanged()
        {
            if (oldParameterVMs is not null)
            {
                oldParameterVMs.CollectionChanged -= ParameterVMs_CollectionChanged;
                foreach (ParameterVM pVM in oldParameterVMs)
                    pVM.PropertyChanged -= ParameterVM_PropertyChanged;
            }
            oldParameterVMs = ParameterVMs;
            ParameterVMs.CollectionChanged += ParameterVMs_CollectionChanged;
            foreach (ParameterVM pVM in ParameterVMs)
                pVM.PropertyChanged += ParameterVM_PropertyChanged;

            ParameterVMs_CollectionChanged(this, new(NotifyCollectionChangedAction.Reset));
        }

        private void ParameterVMs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            redoColumns();
        }

        private void ParameterVM_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // TODO At some point should check that renaming the parameter doesn't actually break things
            if (e.PropertyName == nameof(ParameterVM.Name))
                redoColumns();
        }

        protected void PartVMsPropertyChanged()
        {
            if (oldPartVMs is not null)
            {
                oldPartVMs.CollectionChanged -= PartVMs_CollectionChanged;
                foreach (PartVM pVM in oldPartVMs)
                    pVM.PropertyChanged -= PartVM_PropertyChanged;
            }
            PartVMs.CollectionChanged += PartVMs_CollectionChanged;
            oldPartVMs = PartVMs;
            foreach (PartVM pVM in PartVMs)
                pVM.PropertyChanged += PartVM_PropertyChanged;

            PartVMs_CollectionChanged(this, new(NotifyCollectionChangedAction.Reset));
        }

        private void PartVMs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            redoColumns();
        }

        private void PartVM_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Item[]")
                redoColumns();
        }

        #endregion

        private IEnumerable<ParameterVM>? previousOrderedParameterVMs = null;
        private void redoColumns()
        {
            if (ParameterVMs is not null && PartVMs is not null)
            {
                HashSet<ParameterVM> uniqueParameterVMs = new();
                foreach (PartVM partVM in PartVMs)
                    foreach (ParameterVM parameterVM in partVM.ParameterVMs)
                        uniqueParameterVMs.Add(parameterVM);

                var orderedParameterVMs = ParameterVMs.Where(pVM => uniqueParameterVMs.Contains(pVM));

                if (previousOrderedParameterVMs is null || !orderedParameterVMs.SequenceEqual(previousOrderedParameterVMs))
                {
                    previousOrderedParameterVMs = orderedParameterVMs;

                    dataGrid_Main.Columns.Clear();

                    DataGridTextColumn dataGridTextColumn;
                    DataGridCheckBoxColumn dataGridCheckBoxColumn;
                    dataGridTextColumn = new()
                    {
                        Header = "Part UID",
                        Binding = new Binding($"PartUID")
                    };
                    dataGrid_Main.Columns.Add(dataGridTextColumn);
                    dataGridTextColumn = new()
                    {
                        Header = "Description",
                        Binding = new Binding($"Description")
                    };
                    dataGrid_Main.Columns.Add(dataGridTextColumn);
                    dataGridTextColumn = new()
                    {
                        Header = "Manufacturer",
                        Binding = new Binding($"Manufacturer")
                    };
                    dataGrid_Main.Columns.Add(dataGridTextColumn);
                    dataGridTextColumn = new()
                    {
                        Header = "MPN",
                        Binding = new Binding($"MPN")
                    };
                    dataGrid_Main.Columns.Add(dataGridTextColumn);
                    dataGridTextColumn = new()
                    {
                        Header = "Value",
                        Binding = new Binding($"Value")
                    };
                    dataGrid_Main.Columns.Add(dataGridTextColumn);
                    dataGridCheckBoxColumn = new()
                    {
                        Header = "Exclude BOM",
                        Binding = new Binding($"ExcludeFromBOM")
                    };
                    dataGrid_Main.Columns.Add(dataGridCheckBoxColumn);
                    dataGridCheckBoxColumn = new()
                    {
                        Header = "Exclude Board",
                        Binding = new Binding($"ExcludeFromBoard")
                    };
                    dataGrid_Main.Columns.Add(dataGridCheckBoxColumn);
                    foreach (ParameterVM parameterVM in orderedParameterVMs)
                    {
                        dataGridTextColumn = new();
                        dataGridTextColumn.Header = parameterVM.Name;
                        dataGridTextColumn.Binding = new Binding($"[{parameterVM.Name}]");
                        dataGrid_Main.Columns.Add(dataGridTextColumn);
                    }
                }
            }
            else
            {
                dataGrid_Main.Columns.Clear();
            }
        }

        public UserControl_PartGrid()
        {
            InitializeComponent();
        }
    }
}
