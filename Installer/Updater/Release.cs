using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeckleUpdater
{
  public class Release
  {
    public string Name { get; set; }
    public string Url { get; set; }
    public string FileName { get; set; }

    public Release(string name, string url)
    {
      Name = name;
      FileName = $"{Globals.InstallerName}-v{name}.exe";
      Url = url;
    }
  }
}
