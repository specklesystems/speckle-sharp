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
using System.Windows.Shapes;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using Speckle.DesktopUI.Utils;

namespace Speckle.DesktopUI
{
  /// <summary>
  /// Interaction logic for RootView.xaml
  /// </summary>
  public partial class RootView : Window
  {
    public RootView()
    {
      if (Application.Current == null)
      {
        //if the app is null, eg revit, make one
        new Application();
      }

      //manually inject our main resource dic
      //we can't put it in app.xml since this window can be loaded by another app
      InitializeMaterialDesign();
      Application.Current.Resources.MergedDictionaries.Add(
        Application.LoadComponent(
          new Uri("SpeckleDesktopUI;component/Themes/Generic.xaml", UriKind.Relative)
        ) as ResourceDictionary);

      InitializeComponent();
    }
    // default bindings to null if none are passed
    //public MainWindow() : this(null) { }

    private void InitializeMaterialDesign()
    {
      // Create dummy objects to force the MaterialDesign assemblies to be loaded
      // from this assembly
      // https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/issues/1249
      var card = new Card();
      var hue = new Hue("Dummy", Colors.Black, Colors.White);
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

    private void MenuToggleButton_OnClick(object sender, RoutedEventArgs e) => NavDrawerListBox.Focus();

    private void MenuDarkModeButton_Click(object sender, RoutedEventArgs e) => ModifyTheme(DarkModeToggleButton.IsChecked == true);
    private static void ModifyTheme(bool isDarkTheme)
    {
      PaletteHelper paletteHelper = new PaletteHelper();
      ITheme theme = paletteHelper.GetTheme();

      theme.SetBaseTheme(isDarkTheme ? Theme.Dark : Theme.Light);

      paletteHelper.SetTheme(theme);
    }
  }
}
