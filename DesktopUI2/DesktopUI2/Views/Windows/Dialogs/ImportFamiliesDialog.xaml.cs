using Avalonia;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using ReactiveUI;
using Speckle.Core.Logging;
using System.Collections.Generic;
using Avalonia.Interactivity;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace DesktopUI2.Views.Windows.Dialogs
{
  public partial class ImportFamiliesDialog : ReactiveUserControl<ImportFamiliesDialogViewModel>, ICloseable
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

    public ImportFamiliesDialog()
    {
      InitializeComponent();
    }

    private void InitializeComponent()
    {
      AvaloniaXamlLoader.Load(this);
    }

    public static ImportFamiliesDialog Instance { get; private set; }

  }
}