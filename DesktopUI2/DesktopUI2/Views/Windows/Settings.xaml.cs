using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Diagnostics;

namespace DesktopUI2.Views.Windows
{
  // This is for storing custom application settings for sending and receiving
  public partial class Settings : Window
  {
    public Settings()
    {

      //AvaloniaXamlLoader.Load(this);
    }
    private void Close_Click(object sender, RoutedEventArgs e)
    {
      this.Close();
    }
  }
}
