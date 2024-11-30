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
    public interface IBasicCommand : ICommand
    {
        void RaiseCanExecuteChanged();
    }

    public abstract class BasicCommandBase : IBasicCommand
    {
        public abstract bool CanExecute(object? parameter);
        public abstract void Execute(object? parameter);

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

    public class BasicCommand : BasicCommandBase
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public BasicCommand(Action<object?> execute, Predicate<object?>? canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public override bool CanExecute(object? parameter)
        {
            if (_canExecute is null)
                return true;
            return _canExecute(parameter);
        }

        public override void Execute(object? parameter)
        {
            _execute(parameter);
        }
    }
}
