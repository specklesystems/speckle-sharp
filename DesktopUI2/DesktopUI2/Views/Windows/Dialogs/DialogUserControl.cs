#nullable enable
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using DesktopUI2.ViewModels;

namespace DesktopUI2.Views.Windows.Dialogs;

public class DialogUserControl : UserControl, ICloseable
{
  private object? _dialogResult;

  public event EventHandler Closed;

  private IDialogHost _host;

  public Task ShowDialog(IDialogHost host = null)
  {
    return ShowDialog<object>(host);
  }

  public Task<TResult> ShowDialog<TResult>(IDialogHost host = null)
  {
    _host = host;

    if (_host == null && MainViewModel.Instance != null)
      _host = MainViewModel.Instance;

    _host.DialogBody = this;

    var result = new TaskCompletionSource<TResult>();

    Observable
      .FromEventPattern<EventHandler, EventArgs>(x => Closed += x, x => Closed -= x)
      .Take(1)
      .Subscribe(_ =>
      {
        result.SetResult((TResult)(_dialogResult ?? default(TResult)!));
      });

    return result.Task;
  }

  public void Close(object dialogResult)
  {
    _dialogResult = dialogResult;
    _host.DialogBody = null;
    Closed?.Invoke(this, null);
  }
}
