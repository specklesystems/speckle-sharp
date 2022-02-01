using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Speckle.DesktopUI.Settings;
using Speckle.DesktopUI.Utils;
using Stylet;
using SpeckleSettings = Speckle.DesktopUI.Settings;

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
      Globals.RootResourceDict = Resources;
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
  }
}
