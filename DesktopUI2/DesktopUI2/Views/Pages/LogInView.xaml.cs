using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using ReactiveUI;

namespace DesktopUI2.Views.Pages
{
  public partial class LogInView : ReactiveUserControl<LogInViewModel>
  {

    public LogInView()
    {
      this.WhenActivated(disposables => { });
      AvaloniaXamlLoader.Load(this);
    }
  }
}
