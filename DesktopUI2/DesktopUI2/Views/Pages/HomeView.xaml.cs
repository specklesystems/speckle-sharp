using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using ReactiveUI;
using Speckle.Core.Api;

namespace DesktopUI2.Views.Pages
{
  public partial class HomeView : ReactiveUserControl<HomeViewModel>
  {
    public HomeView()
    {
      this.WhenActivated(disposables => { });
      AvaloniaXamlLoader.Load(this);
    }


  }
}
