using System;
using System.Windows.Input;

namespace ConnectorGSA.Utilities
{
  public abstract class DelegateCommandBase
  {
    public event EventHandler CanExecuteChanged;

    public void RaiseCanExecuteChanged()
    {
      CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
  }

  public class DelegateCommand<T> : DelegateCommandBase, ICommand where T : class
  {
    private readonly Predicate<T> _canExecute;
    private readonly Action<T> _execute;

    public DelegateCommand(Action<T> execute)
        : this(execute, null)
    {
    }

    public DelegateCommand(Action<T> execute, Predicate<T> canExecute)
    {
      _execute = execute;
      _canExecute = canExecute;
    }

    public bool CanExecute(object parameter)
    {
      if (_canExecute == null)
        return true;

      return _canExecute((T)parameter);
    }

    public void Execute(object parameter)
    {
      _execute((T)parameter);
    }
  }
}
