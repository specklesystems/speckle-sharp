using System.Diagnostics;
using System.IO;

namespace Speckle.Core.Helpers;

public static class Open
{
  public static void Url(string url)
  {
    var psi = new ProcessStartInfo { FileName = url, UseShellExecute = true };
    Process.Start(psi);
  }

  public static void File(string path, string? arguments = null)
  {
    var psi = new ProcessStartInfo { FileName = path, UseShellExecute = true };
    FileAttributes attr = System.IO.File.GetAttributes(path);
    if (attr.HasFlag(FileAttributes.Directory))
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
