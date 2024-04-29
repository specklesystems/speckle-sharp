namespace Speckle.DllConflictManagement.EventEmitter;

public class ActionEventArgs : EventArgs
{
  public ActionEventArgs(string eventName, Dictionary<string, object?>? eventProperties)
  {
    EventName = eventName;
    EventProperties = eventProperties;
  }

  public string EventName { get; }
  public Dictionary<string, object?>? EventProperties { get; }
}
