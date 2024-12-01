using KiCAD_DB_Editor.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace KiCAD_DB_Editor.View.Converters
{
    class ParameterVMCollectionDifference_Multiconverter : IMultiValueConverter, IDisposable
    {
        private bool disposedValue;

        private ObservableCollectionEx<ParameterVM>? AllParameterVMs;
        private ObservableCollectionEx<ParameterVM>? InheritedParameterVMs;
        private ObservableCollectionEx<ParameterVM>? SubsetParameterVMs;
        private ObservableCollectionEx<ParameterVM> DifferenceParameterVMs = new();

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length == 3)
            {
                if (values[0] is ObservableCollectionEx<ParameterVM> allParameterVMs &&
                    values[1] is ObservableCollectionEx<ParameterVM> inheritedParameterVMs &&
                    values[2] is ObservableCollectionEx<ParameterVM> subsetParameterVMs)
                {
                    if (AllParameterVMs is not null) AllParameterVMs.CollectionChanged -= AllParameterVMs_CollectionChanged;
                    AllParameterVMs = allParameterVMs;
                    AllParameterVMs.CollectionChanged += AllParameterVMs_CollectionChanged;

                    if (InheritedParameterVMs is not null) InheritedParameterVMs.CollectionChanged -= InheritedParameterVMs_CollectionChanged;
                    InheritedParameterVMs = inheritedParameterVMs;
                    InheritedParameterVMs.CollectionChanged += InheritedParameterVMs_CollectionChanged;

                    if (SubsetParameterVMs is not null) SubsetParameterVMs.CollectionChanged -= SubsetParameterVMs_CollectionChanged;
                    SubsetParameterVMs = subsetParameterVMs;
                    SubsetParameterVMs.CollectionChanged += SubsetParameterVMs_CollectionChanged;

                    DifferenceParameterVMs = new(AllParameterVMs.Except(InheritedParameterVMs).Except(SubsetParameterVMs));

                    return DifferenceParameterVMs;
                }
            }
            return DependencyProperty.UnsetValue;
        }

        private void SubsetParameterVMs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            DifferenceParameterVMs.ExternalCollectionChanged(new(NotifyCollectionChangedAction.Reset));
        }

        private void InheritedParameterVMs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            DifferenceParameterVMs.ExternalCollectionChanged(new(NotifyCollectionChangedAction.Reset));
        }

        private void AllParameterVMs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            DifferenceParameterVMs.ExternalCollectionChanged(new(NotifyCollectionChangedAction.Reset));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    if (AllParameterVMs is not null) AllParameterVMs.CollectionChanged -= AllParameterVMs_CollectionChanged;
                    if (InheritedParameterVMs is not null) InheritedParameterVMs.CollectionChanged -= InheritedParameterVMs_CollectionChanged;
                    if (SubsetParameterVMs is not null) SubsetParameterVMs.CollectionChanged -= SubsetParameterVMs_CollectionChanged;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
