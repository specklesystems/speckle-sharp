using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DesktopUI2.Views.Controls.StreamEditControls;

public class Report : UserControl
{
  public Report()
  {
    InitializeComponent();
  }

  private void InitializeComponent()
  {
    AvaloniaXamlLoader.Load(this);
  }
}
