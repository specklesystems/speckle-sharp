using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DesktopUI2.Views.Controls.StreamEditControls;

public class Activity : UserControl
{
  public Activity()
  {
    InitializeComponent();
  }

  private void InitializeComponent()
  {
    AvaloniaXamlLoader.Load(this);
  }
}
