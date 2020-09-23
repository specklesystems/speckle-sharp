using System;
using System.Windows;
using Speckle.Core.Api;
using Speckle.DesktopUI.Utils;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class StreamViewModel : Screen
  {
    private IEventAggregator _events;
    public StreamState State { get; set; }
    public Stream Stream { get; set; }
    public Branch Branch { get; set; }

    public StreamViewModel(IEventAggregator events)
    {
      _events = events;
    }

    // TODO figure out how to call this from parent instead of
    // rewriting the method here
    public void CopyStreamId(string streamId)
    {
      Clipboard.SetText(streamId);
      _events.Publish(new ShowNotificationEvent()
      {
        Notification = "Stream ID copied to clipboard!"
      });
    }

    public void OpenHelpLink(string url)
    {
      Link.OpenInBrowser(url);
    }
  }
}
