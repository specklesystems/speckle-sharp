using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.UI;
using CefSharp;
using System.Windows.Threading;

namespace Speckle.ConnectorRevitDUI3;

public partial class CefSharpPanel : Page, Autodesk.Revit.UI.IDockablePaneProvider
{
  public CefSharpPanel()
  {
    InitializeComponent();
  }

  public void ExecuteScriptAsync(string script)
  {
    Browser.Dispatcher.Invoke(() => Browser.ExecuteScriptAsync(script), DispatcherPriority.Background);
  }

  public void ShowDevTools()
  {
    Browser.ShowDevTools();
  }
  
  public void SetupDockablePane(Autodesk.Revit.UI.DockablePaneProviderData data)
  {
    data.FrameworkElement = this as FrameworkElement;
    data.InitialState = new Autodesk.Revit.UI.DockablePaneState();
    data.InitialState.DockPosition = DockPosition.Tabbed;
    data.InitialState.TabBehind = Autodesk.Revit.UI.DockablePanes.BuiltInDockablePanes.ProjectBrowser;
  }
}

