using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DesktopUI2.Views.Settings
{
  public partial class NumericUpDownSettingView : UserControl
  {
    public NumericUpDownSettingView()
    {
      InitializeComponent();
    }

    private void InitializeComponent()
    {
      AvaloniaXamlLoader.Load(this);
    }
  }
}
