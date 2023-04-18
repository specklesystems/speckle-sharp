using System;
using ReactiveUI;
using Speckle.Core.Logging;
using Splat;

namespace DesktopUI2.ViewModels;

public class LogInViewModel : ReactiveObject, IRoutableViewModel
{
  public LogInViewModel(IScreen screen)
  {
    try
    {
      HostScreen = screen;
      Bindings = Locator.Current.GetService<ConnectorBindings>();
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Fatal(
        ex,
        "Failed to construct view model {viewModel} {exceptionMessage}",
        GetType(),
        ex.Message
      );
    }
  }

  public ConnectorBindings Bindings { get; private set; } = new DummyBindings();
  public string UrlPathSegment => "login";

  public IScreen HostScreen { get; }

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
    await Utils.AddAccountCommand().ConfigureAwait(true);
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
}
