using System.Diagnostics;

namespace Speckle.Core.Helpers;

public static class Open
{
  public static void Url(string url)
  {
    var psi = new ProcessStartInfo
    {
      FileName = url,
      UseShellExecute = true
    };
    try {
      Process.Start(psi);
    } catch {
      psi.UseShellExecute = false;
      Process.Start(psi);
    }
  }

  public static void File(string path, string? arguments = null)
  {
    var psi = new ProcessStartInfo { FileName = path, UseShellExecute = true };
    try
    {
      System.IO.FileAttributes attr = System.IO.File.GetAttributes(path);
      if ((attr & System.IO.FileAttributes.Directory) == System.IO.FileAttributes.Directory)
      {
        Process.Start(psi);
      }
      else
      {
        if (!string.IsNullOrWhiteSpace(arguments))
        {
          psi.Arguments = arguments;
        }
        Process.Start(psi);
      }
    }
    catch
    {
      psi.UseShellExecute = false;
      System.IO.FileAttributes attr = System.IO.File.GetAttributes(path);
      if ((attr & System.IO.FileAttributes.Directory) == System.IO.FileAttributes.Directory)
      {
        Process.Start(psi);
      }
      else
      {
        if (!string.IsNullOrWhiteSpace(arguments))
        {
          psi.Arguments = arguments;
        }
        Process.Start(psi);
      }
    }
  }
}
