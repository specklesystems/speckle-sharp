namespace AvaloniaHwndHost
{
  using System;
  using System.Runtime.InteropServices;

  internal static unsafe class UnmanagedMethods
  {
    [DllImport("user32.dll")]
    public static extern bool SetParent(IntPtr hWnd, IntPtr hWndNewParent);
  }
}
