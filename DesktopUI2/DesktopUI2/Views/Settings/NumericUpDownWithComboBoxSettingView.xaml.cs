using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DesktopUI2.Views.Settings
{
  public partial class NumericUpDownWithComboBoxSettingView : UserControl
  {
    public NumericUpDownWithComboBoxSettingView()
    {
      InitializeComponent();
    }

    private void InitializeComponent()
    {
      AvaloniaXamlLoader.Load(this);
    }
  }
}
