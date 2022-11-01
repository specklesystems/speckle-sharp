using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using Speckle.Core.Credentials;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace DesktopUI2.Views.Windows.Dialogs
{
  public partial class MappingViewDialog : ReactiveUserControl<MappingViewModel>, ICloseable
  {
    #region DialogUserControlSettings

    private object? _dialogResult;

    public event EventHandler Closed;


    public Task ShowDialog()
    {
      return ShowDialog<object>();
    }

    public Task<TResult> ShowDialog<TResult>()
    {
      MainViewModel.Instance.DialogBody = this;
      Instance = this;

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

      // wait for file dialog (if the user is importing types) to close before calling
      // "MainViewModel.Instance.DialogBody = null"

    }
    #endregion
    public MappingViewDialog()
    {
      InitializeComponent();
    }

    private void InitializeComponent()
    {
      AvaloniaXamlLoader.Load(this);
    }

    public static MappingViewDialog Instance { get; private set; }

  }
}
