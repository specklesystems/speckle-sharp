using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace AvaloniaHwndHost;

internal static class UnmanagedMethods
{
  [DllImport("user32.dll")]
  [SuppressMessage("Security", "CA5392:Use DefaultDllImportSearchPaths attribute for P/Invokes")]
  public static extern bool SetParent(IntPtr hWnd, IntPtr hWndNewParent);
}
