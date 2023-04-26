using System;
using System.Runtime.InteropServices;

namespace AvaloniaHwndHost;

internal static class UnmanagedMethods
{
  [DllImport("user32.dll")]
  public static extern bool SetParent(IntPtr hWnd, IntPtr hWndNewParent);
}
