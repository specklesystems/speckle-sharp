using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using ReactiveUI;

namespace DesktopUI2.Views.Pages
{
  public partial class NotificationsView : ReactiveUserControl<NotificationsViewModel>
  {
    public NotificationsView()
    {
      this.WhenActivated(disposables => { });
      AvaloniaXamlLoader.Load(this);
    }
  }
}
