using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Diagnostics;

namespace DesktopUI2.Views.Windows
{
  public partial class Report : Window
  {
    public Report()
    {
      AvaloniaXamlLoader.Load(this);
#if DEBUG
      this.AttachDevTools();
#endif
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
      this.Close();
    }
  }
}
