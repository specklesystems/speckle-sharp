using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Speckle.DesktopUI
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();
    }

    private void UIElement_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      //until we had a StaysOpen glag to Drawer, this will help with scroll bars
      var dependencyObject = Mouse.Captured as DependencyObject;
      while (dependencyObject != null)
      {
        //if (dependencyObject is ScrollBar) return;
        dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
      }

      MenuToggleButton.IsChecked = false;
    }

    private void MenuToggleButton_OnClick(object sender, RoutedEventArgs e)
        => NavDrawerListBox.Focus();

    private void MenuDarkModeButton_Click(object sender, RoutedEventArgs e)
        => ModifyTheme(DarkModeToggleButton.IsChecked == true);
    private static void ModifyTheme(bool isDarkTheme)
    {
      PaletteHelper paletteHelper = new PaletteHelper();
      ITheme theme = paletteHelper.GetTheme();

      theme.SetBaseTheme(isDarkTheme ? Theme.Dark : Theme.Light);

      paletteHelper.SetTheme(theme);
    }
  }
}
