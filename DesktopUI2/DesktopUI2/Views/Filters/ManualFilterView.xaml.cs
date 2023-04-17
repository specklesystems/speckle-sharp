using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DesktopUI2.Views.Filters;

public class ManualFilterView : UserControl
{
  public ManualFilterView()
  {
    InitializeComponent();
  }

  private void InitializeComponent()
  {
    AvaloniaXamlLoader.Load(this);
  }
}
