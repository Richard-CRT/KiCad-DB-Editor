using KiCAD_DB_Editor.ViewModel;
using System.Text;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowVM? _mWVM;

        public MainWindow()
        {
            InitializeComponent();

            if (DataContext is MainWindowVM mWVM)
            {
                _mWVM = mWVM;
                _mWVM.OnRequestClose += (s, e) => this.Close();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _mWVM?.WindowLoaded();
        }
    }
}