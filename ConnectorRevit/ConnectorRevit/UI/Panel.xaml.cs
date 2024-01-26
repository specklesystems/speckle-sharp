using System;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.UI;
using DesktopUI2.Views;

namespace Speckle.ConnectorRevit;

/// <summary>
/// Interaction logic for Page1.xaml
/// </summary>
public partial class Panel : Page, Autodesk.Revit.UI.IDockablePaneProvider
{
  public Panel()
  {
    InitializeComponent();
    AvaloniaHost.MessageHook += AvaloniaHost_MessageHook;
  }

  private const UInt32 DLGC_WANTARROWS = 0x0001;
  private const UInt32 DLGC_HASSETSEL = 0x0008;
  private const UInt32 DLGC_WANTCHARS = 0x0080;
  private const UInt32 WM_GETDLGCODE = 0x0087;

  /// <summary>
  /// WPF was handling all the text input events and they where not being passed to the Avalonia control
  /// This ensures they are passed, see: https://github.com/AvaloniaUI/Avalonia/issues/8198#issuecomment-1168634451
  /// </summary>
  private IntPtr AvaloniaHost_MessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
  {
    if (msg != WM_GETDLGCODE)
    {
      return IntPtr.Zero;
    }

    handled = true;
    return new IntPtr(DLGC_WANTCHARS | DLGC_WANTARROWS | DLGC_HASSETSEL);
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
