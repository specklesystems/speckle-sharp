using System;
using System.Collections.Generic;
using System.Reactive;
using ReactiveUI;
using Speckle.Core.Logging;
using Splat;

namespace DesktopUI2.ViewModels;

public class NotificationsViewModel : ReactiveObject, IRoutableViewModel
{
  private List<NotificationViewModel> _notifications;

  public NotificationsViewModel(IScreen screen, List<NotificationViewModel> notifications)
  {
    try
    {
      HostScreen = screen;
      Bindings = Locator.Current.GetService<ConnectorBindings>();
      Notifications = notifications;
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

  public ReactiveCommand<Unit, Unit> GoBack => MainViewModel.RouterInstance.NavigateBack;
  public ConnectorBindings Bindings { get; private set; } = new DummyBindings();

  public List<NotificationViewModel> Notifications
  {
    get => _notifications;
    private set => this.RaiseAndSetIfChanged(ref _notifications, value);
  }

  public IScreen HostScreen { get; }

  public string UrlPathSegment { get; } = "notifications";
}
