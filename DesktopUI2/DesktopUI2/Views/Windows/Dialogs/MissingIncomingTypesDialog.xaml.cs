using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace DesktopUI2.Views.Windows.Dialogs;

public class MissingIncomingTypesDialog : DialogUserControl
{
  public MissingIncomingTypesDialog()
  {
    InitializeComponent();
  }

  private void InitializeComponent()
  {
    AvaloniaXamlLoader.Load(this);
  }

  public void MapCommand(object sender, RoutedEventArgs args)
  {
    Close(true);
  }

  public void IgnoreCommand(object sender, RoutedEventArgs e)
  {
    Close(false);
  }
}
