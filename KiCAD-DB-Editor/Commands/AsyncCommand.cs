using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace KiCAD_DB_Editor.Commands
{
    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync(object? parameter);
        void RaiseCanExecuteChanged();
    }

    public abstract class AsyncCommandBase : IAsyncCommand
    {
        public abstract bool CanExecute(object? parameter);
        public abstract Task ExecuteAsync(object? parameter);
        public async void Execute(object? parameter)
        {
            await ExecuteAsync(parameter);
        }
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        public void RaiseCanExecuteChanged()
        {
            Application.Current.Dispatcher.BeginInvoke(CommandManager.InvalidateRequerySuggested);
        }
    }

    public class AsyncCommand : AsyncCommandBase
    {
        private readonly Func<object?, Task> _command;
        private readonly Predicate<object?>? _canExecute;
        public AsyncCommand(Func<object?, Task> command, Predicate<object?>? canExecute)
        {
            _command = command;
            _canExecute = canExecute;
        }
        public override bool CanExecute(object? parameter)
        {
            if (_canExecute is null)
                return true;
            return _canExecute(parameter);
        }
        public override Task ExecuteAsync(object? parameter)
        {
            return _command(parameter);
        }
    }
}
