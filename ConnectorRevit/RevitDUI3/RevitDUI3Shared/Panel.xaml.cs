using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.UI;

namespace Speckle.ConnectorRevitDUI3;

public partial class Panel : Page, Autodesk.Revit.UI.IDockablePaneProvider
{
  public Panel()
  {
    InitializeComponent();
  }

  public void SetupDockablePane(Autodesk.Revit.UI.DockablePaneProviderData data)
  {
    data.FrameworkElement = this as FrameworkElement;
    data.InitialState = new Autodesk.Revit.UI.DockablePaneState();
    data.InitialState.DockPosition = DockPosition.Tabbed;
    data.InitialState.TabBehind = Autodesk.Revit.UI.DockablePanes.BuiltInDockablePanes.ProjectBrowser;
  }
}

