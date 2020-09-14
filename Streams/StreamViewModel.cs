using System;
using Speckle.Core.Api;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class StreamViewModel : Screen
  {
    public Stream Stream { get; set; }
    public Branch Branch { get; set; }

    public StreamViewModel()
    {

    }

    public void OpenHelpLink(string url)
    {
      Link.OpenInBrowser(url);
    }
  }
}