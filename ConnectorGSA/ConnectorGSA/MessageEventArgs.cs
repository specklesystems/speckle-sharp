using Speckle.GSA.API;
using System;

namespace ConnectorGSA
{
  public class MessageEventArgs : EventArgs
  {
    public string[] MessagePortions { get; set; }
    public MessageIntent Intent { get; }
    public MessageLevel Level { get; }

    public Exception Exception { get; }

    public MessageEventArgs(MessageIntent intent, MessageLevel level, Exception ex, params string[] messagePortions)
    {
      this.MessagePortions = messagePortions;
      this.Intent = intent;
      this.Level = level;
      if (ex != null)
      {
        this.Exception = ex;
      }
    }

    public MessageEventArgs(MessageIntent intent, MessageLevel level, params string[] messagePortions)
    {
      this.MessagePortions = messagePortions;
      this.Intent = intent;
      this.Level = level;
    }
  }
}
