using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace DesktopUI2.Views.Windows.Dialogs;

public class QuickOpsDialog : Window
{
  public QuickOpsDialog()
  {
    InitializeComponent();
#if DEBUG
    this.AttachDevTools();
#endif
  }

  private void InitializeComponent()
  {
    AvaloniaXamlLoader.Load(this);
  }

  public void Close_Click(object sender, RoutedEventArgs e)
  {
    Close();
  }
}
