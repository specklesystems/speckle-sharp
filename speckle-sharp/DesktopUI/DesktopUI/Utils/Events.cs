using System.Collections.Generic;
using Speckle.Core.Api;
using Speckle.Core.Models;

namespace Speckle.DesktopUI.Utils
{
  public class EventBase
  {
    public string Notification { get; set; }
    public dynamic DynamicInfo { get; set; }
  }

  public class ShowNotificationEvent : EventBase
  {
  }

  public class ReloadRequestedEvent : EventBase
  {
  }

  public class StreamAddedEvent : EventBase
  {
    public StreamState NewStream { get; set; }
  }

  public class StreamUpdatedEvent : EventBase
  {
    public StreamUpdatedEvent(Stream stream)
    {
      Stream = stream;
      StreamId = stream.id;
    }
    public Stream Stream { get; set; }
    public string StreamId { get; set; }
  }

  public class StreamRemovedEvent : EventBase
  {
    public string StreamId { get; set; }
  }

  public class UpdateSelectionCountEvent : EventBase
  {
    public int SelectionCount { get; set; }
  }

  public class UpdateSelectionEvent : EventBase
  {
    public List<string> ObjectIds { get; set; }
  }

  public class RetrievedFilteredObjectsEvent : EventBase
  {
   // public string UserId { get; set; }
    public IEnumerable<Base> Objects { get; set; }
  }

  public class ApplicationEvent : EventBase
  {
    public enum EventType
    {
      ViewActivated, // is this Revit specific?
      DocumentOpened,
      DocumentClosed,
      DocumentModified,
      ApplicationIdling
    }

    public EventType Type { get; set; }
  }
}
