using System;
using Autodesk.Navisworks.Api;
using DesktopUI2.Views;

namespace Speckle.ConnectorNavisworks.Entry;

public partial class SpeckleHostPane
{
  private const uint DlgcWantarrows = 0x0001;
  private const uint DlgcHasSetSel = 0x0008;
  private const uint DlgcWantChars = 0x0080;
  private const uint WmGetDlgCode = 0x0087;

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

  private static IntPtr AvaloniaHost_MessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
  {
    if (msg != WmGetDlgCode)
      return IntPtr.Zero;
    handled = true;
    return new IntPtr(DlgcWantChars | DlgcWantarrows | DlgcHasSetSel);
  }
}
