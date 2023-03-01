using System;
using System.Windows.Controls;
using Autodesk.Navisworks.Api;
using DesktopUI2.Views;

namespace Speckle.ConnectorNavisworks.Entry
{
  public partial class SpeckleHostPane : UserControl
  {
    private const uint DLGC_WANTARROWS = 0x0001;
    private const uint DLGC_HASSETSEL = 0x0008;
    private const uint DLGC_WANTCHARS = 0x0080;
    private const uint WM_GETDLGCODE = 0x0087;

    public SpeckleHostPane()
    {
      InitializeComponent();

      AvaloniaHost.MessageHook += AvaloniaHost_MessageHook;
      AvaloniaHost.Content = new MainUserControl();

      Application.ActiveDocument.FileNameChanged += Application_DocumentChanged;
    }


    // Triggered when the active document name is changed. This will happen automatically if a document is newly created or opened.
    private void Application_DocumentChanged(object sender, EventArgs e)
    {
      AvaloniaHost.Content = new MainUserControl();
    }

    private static IntPtr AvaloniaHost_MessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam,
      ref bool handled)
    {
      if (msg != WM_GETDLGCODE) return IntPtr.Zero;
      handled = true;
      return new IntPtr(DLGC_WANTCHARS | DLGC_WANTARROWS | DLGC_HASSETSEL);
    }
  }
}