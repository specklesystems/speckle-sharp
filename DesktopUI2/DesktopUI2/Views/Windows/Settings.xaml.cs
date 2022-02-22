using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DesktopUI2.ViewModels;
using System;
using System.Diagnostics;

namespace DesktopUI2.Views.Windows
{
  public partial class Settings : Window
  {
    public Settings()
    {
      AvaloniaXamlLoader.Load(this);
    }
  }
}
