using System;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.UI;
using DesktopUI2.Views;

namespace Speckle.ConnectorRevit
{
  /// <summary>
  /// Interaction logic for Page1.xaml
  /// </summary>
  public partial class Panel : Page, Autodesk.Revit.UI.IDockablePaneProvider
  {
    public Panel()
    {
      InitializeComponent();

    }

    /// <summary>
    /// Switching documents in Revit causes the Panel content to "reset", so we need to re-nitialize the avalonia host each time
    /// </summary>
    public void Init()
    {
      AvaloniaHost.Content = new MainUserControl();
    }



    public void SetupDockablePane(Autodesk.Revit.UI.DockablePaneProviderData data)
    {
      data.FrameworkElement = this as FrameworkElement;
      data.InitialState = new Autodesk.Revit.UI.DockablePaneState();
      data.InitialState.DockPosition = DockPosition.Tabbed;
      data.InitialState.TabBehind = Autodesk.Revit.UI.DockablePanes.BuiltInDockablePanes.ProjectBrowser;
    }
  }
}
