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
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // ClickOnce publishing requires strong name for ApplicationSettingsBase.Upgrade to work
            // https://learn.microsoft.com/en-us/visualstudio/deployment/clickonce-and-application-settings?view=vs-2022#net-core-and-net-5
            // Copy user settings from previous application version only if UpdateSettings is True (default)
            // UpdateSettings check necessary, or it replaces current version settings with previous version settings
            if (KiCad_DB_Editor.Properties.Settings.Default.UpdateSettings)
            {
                // Upgrade user settings from previous installation
                KiCad_DB_Editor.Properties.Settings.Default.Upgrade();
                KiCad_DB_Editor.Properties.Settings.Default.UpdateSettings = false;
                KiCad_DB_Editor.Properties.Settings.Default.Save();
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // Ensure user settings are saved
            KiCad_DB_Editor.Properties.Settings.Default.Save();
        }
    }

}
