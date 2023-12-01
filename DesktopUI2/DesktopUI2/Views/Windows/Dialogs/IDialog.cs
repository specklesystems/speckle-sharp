using Avalonia.Controls;

namespace DesktopUI2.Views.Windows.Dialogs;

public interface IDialogHost
{
  public bool DialogVisible { get; }

  public double DialogOpacity { get; }

  public UserControl DialogBody { get; set; }
}
