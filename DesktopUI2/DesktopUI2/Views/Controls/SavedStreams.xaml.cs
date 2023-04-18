using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DesktopUI2.Views.Controls;

public class SavedStreams : UserControl
{
  public SavedStreams()
  {
    InitializeComponent();
  }

  private void InitializeComponent()
  {
    AvaloniaXamlLoader.Load(this);
  }
}
