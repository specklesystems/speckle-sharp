using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;

namespace DesktopUI2.Views.Settings;

public class ListBoxSettingView : ReactiveUserControl<SettingViewModel>
{
  public ListBoxSettingView()
  {
    InitializeComponent();
  }

  private void InitializeComponent()
  {
    AvaloniaXamlLoader.Load(this);
  }
}
