using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DesktopUI2.Views.Controls;

public class CollaboratorsControl : UserControl
{
  public CollaboratorsControl()
  {
    AvaloniaXamlLoader.Load(this);
    Instance = this;
  }

  public static CollaboratorsControl Instance { get; private set; }
}
