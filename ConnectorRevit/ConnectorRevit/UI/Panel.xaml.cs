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
    #region Data
    private Guid m_targetGuid;
    private DockPosition m_position = DockPosition.Bottom;
    private int m_left = 1;
    private int m_right = 1;
    private int m_top = 1;
    private int m_bottom = 1;
    const string _url_tbc = "http://thebuildingcoder.typepad.com";
    const string _url_git = "https://github.com/jeremytammik/DockableDialog";
    #endregion

    public Panel()
    {
      InitializeComponent();

      AvaloniaHost.Content = new MainUserControl();
    }

    public void SetupDockablePane(Autodesk.Revit.UI.DockablePaneProviderData data)
    {
      data.FrameworkElement = this as FrameworkElement;
      data.InitialState = new Autodesk.Revit.UI.DockablePaneState();
      data.InitialState.DockPosition = DockPosition.Tabbed;
      data.InitialState.TabBehind = Autodesk.Revit.UI.DockablePanes.BuiltInDockablePanes.ProjectBrowser;

    }

    public void SetInitialDockingParameters(int left, int right, int top, int bottom, DockPosition position, Guid targetGuid)
    {
      m_position = position;
      m_left = left;
      m_right = right;
      m_top = top;
      m_bottom = bottom;
      m_targetGuid = targetGuid;
    }
  }
}
