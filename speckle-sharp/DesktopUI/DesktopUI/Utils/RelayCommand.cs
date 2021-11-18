using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Speckle.DesktopUI.Utils
{
  public class RelayCommand<T> : ICommand
  {
    Action<T> _TargetExecuteCommand;
    Func<T, bool> _TargetCanExecuteCommand;

    public RelayCommand(Action<T> executeCommand)
    {
      _TargetExecuteCommand = executeCommand;
    }

    public RelayCommand(Action<T> executeCommand, Func<T, bool> canExecuteCommand)
    {
      _TargetExecuteCommand = executeCommand;
      _TargetCanExecuteCommand = canExecuteCommand;
    }

    public void RaiseCanExecuteChanged()
    {
      CanExecuteChanged(this, EventArgs.Empty);
    }
    #region ICommand Members

    bool ICommand.CanExecute(object parameter)
    {
      if (_TargetCanExecuteCommand != null)
      {
        T tparm = (T)parameter;
        return _TargetCanExecuteCommand(tparm);
      }
      if (_TargetExecuteCommand != null)
      {
        return true;
      }
      return false;
    }

    // Beware - should use weak references if command instance lifetime is longer than lifetime of UI objects that get hooked up to command
    // Prism commands solve this in their implementation
    public event EventHandler CanExecuteChanged = delegate { };

    void ICommand.Execute(object parameter)
    {
      _TargetExecuteCommand?.Invoke((T)parameter);
    }
    #endregion
  }
}
