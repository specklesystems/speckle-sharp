using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DesktopUI2.Views.Windows.Dialogs;
using System.Diagnostics;

namespace DesktopUI2.Views.Windows
{
  public partial class Report : DialogUserControl
  {
    public Report()
    {
      AvaloniaXamlLoader.Load(this);

    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
      this.Close(null);
    }
  }
}
