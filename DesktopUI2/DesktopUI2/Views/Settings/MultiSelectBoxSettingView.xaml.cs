using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;

namespace DesktopUI2.Views.Settings;

public class MultiSelectBoxSettingView : ReactiveUserControl<SettingViewModel>
{
  public MultiSelectBoxSettingView()
  {
    InitializeComponent();
  }

  private void InitializeComponent()
  {
    AvaloniaXamlLoader.Load(this);
  }
}
