using DesktopUI2.Views;
using DesktopUI2.Views.Windows.Dialogs;
using ReactiveUI;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Splat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DesktopUI2.ViewModels
{
  public class LogInViewModel : ReactiveObject, IRoutableViewModel
  {
    public string UrlPathSegment => "login";

    public IScreen HostScreen { get; }

    public ConnectorBindings Bindings { get; private set; } = new DummyBindings();


    #region bindings

    public string Title => "for " + Bindings.GetHostAppNameVersion();
    public string Version => "v" + Bindings.ConnectorVersion;

    private bool _isLoggingIn;
    public bool IsLoggingIn
    {
      get => _isLoggingIn;
      private set => this.RaiseAndSetIfChanged(ref _isLoggingIn, value);
    }

    public async void AddAccountCommand()
    {
      IsLoggingIn = true;
      await Utils.AddAccountCommand();
      IsLoggingIn = false;
    }

    public void LaunchManagerCommand()
    {
      Utils.LaunchManager();
    }

    public void RefreshCommand()
    {
      MainViewModel.Instance.NavigateToDefaultScreen();
    }

    #endregion

    public LogInViewModel(IScreen screen)
    {
      try
      {
        HostScreen = screen;
        Bindings = Locator.Current.GetService<ConnectorBindings>();
      }
      catch (Exception ex)
      {
        Log.CaptureException(ex, Sentry.SentryLevel.Error);
      }
    }
  }
}
