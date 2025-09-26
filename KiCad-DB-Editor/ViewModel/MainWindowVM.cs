using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using KiCad_DB_Editor;
using KiCad_DB_Editor.Model;
using KiCad_DB_Editor.Commands;
using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.Windows.Input;
using System.IO;
using System.Windows.Threading;

namespace KiCad_DB_Editor.ViewModel
{
    public class MainWindowVM : NotifyObject
    {
        public event EventHandler? OnRequestClose;
        private DispatcherTimer _autoSaveTimer;

        #region Notify Properties
        public string WindowTitle
        {
            get
            {
                if (Properties.Settings.Default.OpenProjectPath == "")
                    return "KiCad DB Editor";
                else
                    return $"KiCad DB Editor | {Properties.Settings.Default.OpenProjectPath}";
            }
        }

        private ViewModel.LibraryVM? _libraryVM;
        public ViewModel.LibraryVM? LibraryVM
        {
            get { return _libraryVM; }
            set { if (_libraryVM != value) { _libraryVM = value; InvokePropertyChanged(); } }
        }

        #endregion Notify Properties

        public MainWindowVM()
        {
            // Setup commands
            NewLibraryCommand = new BasicCommand(NewLibraryCommandExecuted, null);
            OpenLibraryCommand = new BasicCommand(OpenLibraryCommandExecuted, null);
            SaveLibraryCommand = new BasicCommand(SaveLibraryCommandExecuted, SaveAsLibraryCommandCanExecute); // Note shares SaveAs CanExecute
            SaveAsLibraryCommand = new BasicCommand(SaveAsLibraryCommandExecuted, SaveAsLibraryCommandCanExecute);
            HelpLibraryCommand = new BasicCommand(HelpCommandExecuted, null);
            ExitLibraryCommand = new BasicCommand(ExitCommandExecuted, null);

            _autoSaveTimer = new();
            _autoSaveTimer.Interval = TimeSpan.FromMinutes(5);
            _autoSaveTimer.Tick += _autoSaveTimer_Tick;
            _autoSaveTimer.Start();
        }

        public void WindowLoaded()
        {
            string[] paths = Environment.GetCommandLineArgs();

            if (paths.Length > 1)
                _openProject(paths[1]);

            if (Properties.Settings.Default.OpenProjectPath == "")
            {
                Debug.Assert(NewLibraryCommand.CanExecute(null));
                NewLibraryCommand.Execute(null);
            }

            if (Properties.Settings.Default.OpenProjectPath != "New Project" && File.Exists(Properties.Settings.Default.OpenProjectPath))
            {
                _openProject(Properties.Settings.Default.OpenProjectPath);
            }
            else
            {
                Debug.Assert(NewLibraryCommand.CanExecute(null));
                NewLibraryCommand.Execute(null);
            }
        }

        private void _autoSaveTimer_Tick(object? sender, EventArgs e)
        {
            if (LibraryVM is not null && Properties.Settings.Default.OpenProjectPath != "" && Properties.Settings.Default.OpenProjectPath != "New Project")
            {
                if (!LibraryVM.Library.WriteToFile(Properties.Settings.Default.OpenProjectPath, out string errorMessage, autosave: true))
                {
                    // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
                    (new View.Dialogs.Window_ErrorDialog($"Save failed!{Environment.NewLine}{Environment.NewLine}Details:{Environment.NewLine}{errorMessage}")).ShowDialog();
                    // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
                }
            }
        }

        private void _openProject(string projectPath)
        {
            if (Library.FromFile(projectPath, out Library? library))
            {
                LibraryVM = new(library!);

                Properties.Settings.Default.OpenProjectPath = projectPath;
                Properties.Settings.Default.Save();
                InvokePropertyChanged(nameof(WindowTitle));
            }
            else
            {
                // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
                (new View.Dialogs.Window_ErrorDialog("Load failed!")).ShowDialog();
                // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY

                Debug.Assert(NewLibraryCommand.CanExecute(null));
                NewLibraryCommand.Execute(null);
            }
        }

        #region Commands

        public IBasicCommand NewLibraryCommand { get; }
        public IBasicCommand OpenLibraryCommand { get; }
        public IBasicCommand SaveLibraryCommand { get; }
        public IBasicCommand SaveAsLibraryCommand { get; }
        public IBasicCommand HelpLibraryCommand { get; }
        public IBasicCommand ExitLibraryCommand { get; }

        private void NewLibraryCommandExecuted(object? parameter)
        {
            LibraryVM = new(Library.FromScratch());

            Properties.Settings.Default.OpenProjectPath = "New Project";
            Properties.Settings.Default.Save();
            InvokePropertyChanged(nameof(WindowTitle));
        }

        private void OpenLibraryCommandExecuted(object? parameter)
        {
            // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
            OpenFileDialog openFileDialog = new();
            // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
            openFileDialog.Title = "Open KiCad DB Editor Project File";
            openFileDialog.Filter = "Project file (*.kidbe_proj)|*.kidbe_proj|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                _openProject(openFileDialog.FileName);
            }
        }

        private bool SaveAsLibraryCommandCanExecute(object? parameter)
        {
            return (LibraryVM is not null);
        }

        private void SaveLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(LibraryVM is not null);

            if (Properties.Settings.Default.OpenProjectPath != "" && Properties.Settings.Default.OpenProjectPath != "New Project")
            {
                if (!LibraryVM.Library.WriteToFile(Properties.Settings.Default.OpenProjectPath, out string errorMessage))
                {
                    // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
                    (new View.Dialogs.Window_ErrorDialog($"Save failed!{Environment.NewLine}{Environment.NewLine}Details:{Environment.NewLine}{errorMessage}")).ShowDialog();
                    // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
                }
                else if (LibraryVM.Library.KiCadAutoExportOnSave && !LibraryVM.Library.AutoExportToKiCad(out errorMessage))
                {
                    // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
                    (new View.Dialogs.Window_ErrorDialog($"Export failed!{Environment.NewLine}{Environment.NewLine}Details:{Environment.NewLine}{errorMessage}")).ShowDialog();
                    // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
                }

            }
            else
            {
                // Shares the CanExecute method, so not need to recheck
                SaveAsLibraryCommand.Execute(parameter);
            }
        }

        private void SaveAsLibraryCommandExecuted(object? parameter)
        {
            Debug.Assert(LibraryVM is not null);

            // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
            SaveFileDialog saveFileDialog = new();
            // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
            saveFileDialog.Title = "Save KiCad DB Editor Project File";
            saveFileDialog.Filter = "Project file (*.kidbe_proj)|*.kidbe_proj|All files (*.*)|*.*";
            if (saveFileDialog.ShowDialog() == true)
            {
                if (LibraryVM.Library.WriteToFile(saveFileDialog.FileName, out string errorMessage))
                {
                    Properties.Settings.Default.OpenProjectPath = saveFileDialog.FileName;
                    Properties.Settings.Default.Save();
                    InvokePropertyChanged(nameof(WindowTitle));

                    if (LibraryVM.Library.KiCadAutoExportOnSave && !LibraryVM.Library.AutoExportToKiCad(out errorMessage))
                    {
                        // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
                        (new View.Dialogs.Window_ErrorDialog($"Export failed!{Environment.NewLine}{Environment.NewLine}Details:{Environment.NewLine}{errorMessage}")).ShowDialog();
                        // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
                    }
                }
                else
                {
                    // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
                    (new View.Dialogs.Window_ErrorDialog($"Save failed!{Environment.NewLine}{Environment.NewLine}Details:{Environment.NewLine}{errorMessage}")).ShowDialog();
                    // BREAKS MVVM BUT NOT WORTH THE EFFORT TO DO DIALOGS PROPERLY
                }
            }
        }

        private void HelpCommandExecuted(object? parameter)
        {
            Process.Start(new ProcessStartInfo("https://docs.kicad.org/7.0/en/eeschema/eeschema.html#database-libraries") { UseShellExecute = true });
        }

        private void ExitCommandExecuted(object? parameter)
        {
            this.OnRequestClose?.Invoke(this, new());
        }

        #endregion Commands
    }
}
