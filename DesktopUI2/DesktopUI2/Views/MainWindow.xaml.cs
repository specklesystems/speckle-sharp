using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.ComponentModel;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using ReactiveUI;
using Avalonia.Input;
using Speckle.Core.Logging;
using System.Collections.Generic;

namespace DesktopUI2.Views
{
  public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
  {
    public MainWindow()
    {

      this.WhenActivated(disposables => { });
      AvaloniaXamlLoader.Load(this);
      Instance = this;

      Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() { { "name", "Launched" } });


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
