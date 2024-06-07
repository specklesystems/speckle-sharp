using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using DesktopUI2.ViewModels;
using DesktopUI2.Views;
using Speckle.Core.Logging;

namespace SpeckleRhino;

/// <summary>
/// Interaction logic for Page1.xaml
/// </summary>
[Guid("3EA3FEE1-216D-4076-9A06-949DE4C0E8AF")]
public partial class DuiPanel : UserControl
{
  public DuiPanel()
  {
    try
    {
      InitializeComponent();
      //set here otherwise we get errors about re-used visual parents when closing and re-opening the panel
      //there might be other solutions too. If changing this behaviour make sure to refresh the view model
      //when opening a new file as well
      var viewModel = new MainViewModel(SpeckleRhinoConnectorPlugin.Instance.Bindings);
      DataContext = viewModel;
      AvaloniaHost.Content = new MainUserControl();
      AvaloniaHost.MessageHook += AvaloniaHost_MessageHook;
    }
    catch (Exception ex) when (!ex.IsFatal()) { }
  }

  private const UInt32 DLGC_WANTARROWS = 0x0001;
  private const UInt32 DLGC_HASSETSEL = 0x0008;
  private const UInt32 DLGC_WANTCHARS = 0x0080;
  private const UInt32 WM_GETDLGCODE = 0x0087;

  private IntPtr AvaloniaHost_MessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
  {
    if (msg != WM_GETDLGCODE)
    {
      return IntPtr.Zero;
    }

    handled = true;
    return new IntPtr(DLGC_WANTCHARS | DLGC_WANTARROWS | DLGC_HASSETSEL);
  }
}
