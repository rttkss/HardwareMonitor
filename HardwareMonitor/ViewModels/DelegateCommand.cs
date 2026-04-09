using System;
using System.Windows.Input;

namespace HardwareMonitor
{
    public class DelegateCommand : ICommand
    {
        private readonly Action _run;
        private readonly Func<bool> _canRun;

        public DelegateCommand(Action run)
        {
            _run = run;
            _canRun = null;
        }

        public DelegateCommand(Action run, Func<bool> canRun)
        {
            _run = run;
            _canRun = canRun;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            if (_canRun != null)
            {
                return _canRun();
            }
            return true;
        }

        public void Execute(object parameter)
        {
            _run();
        }
    }
}