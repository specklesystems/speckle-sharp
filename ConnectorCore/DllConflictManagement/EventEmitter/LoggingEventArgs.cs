namespace Speckle.DllConflictManagement.EventEmitter;

public class LoggingEventArgs : EventArgs
{
  public LoggingEventArgs(string contextMessage, Exception? exception = null)
  {
    Exception = exception;
    ContextMessage = contextMessage;
  }

  public Exception? Exception { get; }
  public string ContextMessage { get; }
}
