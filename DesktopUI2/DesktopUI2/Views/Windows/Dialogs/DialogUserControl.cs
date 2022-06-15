using Avalonia.Controls;
using Avalonia.Input;
using DesktopUI2.ViewModels;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopUI2.Views.Windows.Dialogs
{
  public  class DialogUserControl : UserControl, ICloseable
  {
    private object? _dialogResult;

    public event EventHandler Closed;


    public Task ShowDialog()
    {
      return ShowDialog<object>();
    }

    public Task<TResult> ShowDialog<TResult>()
    {
      MainViewModel.Instance.DialogBody = this;

      var result = new TaskCompletionSource<TResult>();

      Observable.FromEventPattern<EventHandler, EventArgs>(
                    x => Closed += x,
                    x => Closed -= x)
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
      Closed?.Invoke(this, null);
      MainViewModel.Instance.DialogBody=null;
    }
  }
}
