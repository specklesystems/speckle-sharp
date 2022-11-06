using Avalonia;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using DesktopUI2.ViewModels.MappingTool;
using ReactiveUI;
using System.ComponentModel;

namespace DesktopUI2.Views
{
  public partial class MappingsWindow : ReactiveWindow<MappingsViewModel>
  {
    public MappingsWindow()
    {

      this.WhenActivated(disposables => { });
      AvaloniaXamlLoader.Load(this);

#if DEBUG
      this.AttachDevTools(KeyGesture.Parse("CTRL+R"));
#endif

    }

    protected override void OnClosing(CancelEventArgs e)
    {
      this.Hide();
      e.Cancel = true;
      base.OnClosing(e);
    }
  }
}
