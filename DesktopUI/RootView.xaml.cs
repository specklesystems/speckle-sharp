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
using SpeckleSettings = Speckle.DesktopUI.Settings;
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
      InitializeComponent();
    }
    // default bindings to null if none are passed
    //public MainWindow() : this(null) { }

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

  }
}
