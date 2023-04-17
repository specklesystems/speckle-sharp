using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using ReactiveUI;

namespace DesktopUI2.Views.Pages;

public class HomeView : ReactiveUserControl<HomeViewModel>
{
  public HomeView()
  {
    this.WhenActivated(disposables => { });
    AvaloniaXamlLoader.Load(this);
  }
}
