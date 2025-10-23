using System.Configuration;
using System.Data;
using System.Windows;

namespace KiCad_DB_Editor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // Ensure user settings are saved
            KiCad_DB_Editor.Properties.Settings.Default.Save();
        }
    }

}
