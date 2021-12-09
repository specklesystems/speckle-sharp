using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;

namespace DesktopUI2.Views.Settings
{
  public partial class DropdownSettingView : ReactiveUserControl<FilterViewModel>
  {
    public DropdownSettingView()
    {
      InitializeComponent();
    }

    private void InitializeComponent()
    {
      AvaloniaXamlLoader.Load(this);
    }
  }
}
