using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
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

      if (Application.Current == null)
      {
        //if the app is null, eg revit, make one
        new Application();
        //make sure material design is loaded
        var type = typeof(PaletteHelper);
      }

      //manually inject our main resource dic
      //we can't put it in app.xml since this window can be loaded by another app
      Application.Current.Resources.MergedDictionaries.Add(
      Application.LoadComponent(
        new Uri("SpeckleDesktopUI;component/Themes/Generic.xaml", UriKind.Relative)
        ) as ResourceDictionary);

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
