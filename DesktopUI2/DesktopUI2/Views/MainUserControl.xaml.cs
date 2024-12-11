using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using ReactiveUI;
using Speckle.Core.Logging;

namespace DesktopUI2.Views;

public class MainUserControl : ReactiveUserControl<MainViewModel>
{
  public static NotificationManager NotificationManager;

  public MainUserControl()
  {
    this.WhenActivated(disposables => { });
    AvaloniaXamlLoader.Load(this);

    NotificationManager = this.FindControl<NotificationManager>("NotificationManager");
  }
}
