using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using ReactiveUI;
using Speckle.Core.Logging;
using System.Collections.Generic;

namespace DesktopUI2.Views
{
  public partial class MainUserControl : ReactiveUserControl<MainViewModel>
  {
    public static NotificationManager NotificationManager;
    public MainUserControl()
    {
      this.WhenActivated(disposables => { });
      AvaloniaXamlLoader.Load(this);

      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Launched" } }, isAction: false);

      NotificationManager = this.FindControl<NotificationManager>("NotificationManager");
    }
  }
}
