namespace Speckle.DllConflictManagement.EventEmitter;

/// <summary>
/// Responsible for emitting events that give details about what is happening in the conflict detection process.
/// All calls to emit an event will actually get added to a list of events to be emitted later. This allow the events
/// to be recorded before the proper event receiver is ready to receive the events. i.e. we can record errors in
/// the dll conflict detection process, but not properly log them until we verify that using our logger isn't going
/// to crash our plugin
/// </summary>
public class DllConflictEventEmitter
{
  public event EventHandler<LoggingEventArgs>? OnError;
  public event EventHandler<LoggingEventArgs>? OnInfo;
  public event EventHandler<ActionEventArgs>? OnAction;

  private bool _shouldEmitEvents;
  private readonly List<LoggingEventArgs> _savedErrorEvents = new();
  private readonly List<LoggingEventArgs> _savedInfoEvents = new();
  private readonly List<ActionEventArgs> _savedActionEvents = new();

  public void EmitError(LoggingEventArgs e)
  {
    if (_shouldEmitEvents)
    {
      OnError?.Invoke(this, e);
    }
    else
    {
      _savedErrorEvents.Add(e);
    }
  }

  public void EmitInfo(LoggingEventArgs e)
  {
    if (_shouldEmitEvents)
    {
      OnInfo?.Invoke(this, e);
    }
    else
    {
      _savedInfoEvents.Add(e);
    }
  }

  public void EmitAction(ActionEventArgs e)
  {
    if (_shouldEmitEvents)
    {
      OnAction?.Invoke(this, e);
    }
    else
    {
      _savedActionEvents.Add(e);
    }
  }

  public void BeginEmit()
  {
    _shouldEmitEvents = true;
    foreach (var e in _savedErrorEvents)
    {
      OnError?.Invoke(this, e);
    }
    foreach (var e in _savedInfoEvents)
    {
      OnInfo?.Invoke(this, e);
    }
    foreach (var e in _savedActionEvents)
    {
      OnAction?.Invoke(this, e);
    }
  }
}
