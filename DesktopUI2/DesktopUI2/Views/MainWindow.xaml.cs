using Avalonia;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using ReactiveUI;
using System.ComponentModel;

namespace DesktopUI2.Views
{
  public partial class MainWindow : ReactiveWindow<MainViewModel>
  {
    public MainWindow()
    {

      this.WhenActivated(disposables => { });
      AvaloniaXamlLoader.Load(this);
      Instance = this;

#if DEBUG
      this.AttachDevTools(KeyGesture.Parse("CTRL+R"));
#endif

    }

    public static MainWindow Instance { get; private set; }


    protected override void OnClosing(CancelEventArgs e)
    {
      this.Hide();
      e.Cancel = true;
      base.OnClosing(e);
    }
  }
}
