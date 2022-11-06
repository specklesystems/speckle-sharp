using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using DesktopUI2.ViewModels.MappingTool;
using ReactiveUI;

namespace DesktopUI2.Views
{
  public partial class MappingsControl : ReactiveUserControl<MappingsViewModel>
  {
    public MappingsControl()
    {
      this.WhenActivated(disposables => { });
      AvaloniaXamlLoader.Load(this);
    }
  }
}
