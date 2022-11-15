using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DesktopUI2.Views.Controls
{
  public partial class CollaboratorsControl : UserControl
  {
    public static CollaboratorsControl Instance { get; private set; }
    public CollaboratorsControl()
    {
      AvaloniaXamlLoader.Load(this);
      Instance = this;
    }
  }
}
