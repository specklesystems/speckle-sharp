using ReactiveUI;
using Speckle.Core.Logging;
using Splat;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;

namespace DesktopUI2.ViewModels
{
  public class NotificationsViewModel : ReactiveObject, IRoutableViewModel
  {
    public IScreen HostScreen { get; }

    public ReactiveCommand<Unit, Unit> GoBack => MainViewModel.RouterInstance.NavigateBack;


    public string UrlPathSegment { get; } = "notifications";
    public ConnectorBindings Bindings { get; private set; } = new DummyBindings();

    private List<NotificationViewModel> _notifications;
    public List<NotificationViewModel> Notifications
    {
      get => _notifications;
      private set => this.RaiseAndSetIfChanged(ref _notifications, value);

    }

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
        Log.CaptureException(ex, Sentry.SentryLevel.Error);
      }
    }
  }
}
