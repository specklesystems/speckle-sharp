using Speckle.GSA.API;
using System;
using System.Collections.Generic;

namespace ConnectorGSA.Utilities
{
  public interface ISpeckleAppMessenger
  {
    bool Message(MessageIntent intent, MessageLevel level, params string[] messagePortions);
    bool Message(MessageIntent intent, MessageLevel level, Exception ex, params string[] messagePortions);
  }

  //This is mainly for use in the SpeckleInterface library, not the kit
  public class ProgressMessenger : ISpeckleAppMessenger
  {
    private readonly IProgress<MessageEventArgs> loggingProgress;

    //These are meant to be in corresponding order so that an index of one used in looking up the other correlates correct pairs
    private static readonly List<Speckle.GSA.API.MessageIntent> AppIntent = new List<MessageIntent>() 
      { MessageIntent.Display, MessageIntent.TechnicalLog, MessageIntent.Telemetry };
    private static readonly List<MessageIntent> InterfaceIntent = new List<MessageIntent>()  { MessageIntent.Display, MessageIntent.TechnicalLog, MessageIntent.Telemetry };

    private static readonly List<MessageLevel> AppLevel = new List<MessageLevel>()
      { MessageLevel.Information, MessageLevel.Debug, MessageLevel.Error, MessageLevel.Fatal };
    private static readonly List<MessageLevel> InterfaceLevel = new List<MessageLevel>()  { MessageLevel.Information, MessageLevel.Debug, MessageLevel.Error, MessageLevel.Fatal };

    public ProgressMessenger(IProgress<MessageEventArgs> loggingProgress)
    {
      this.loggingProgress = loggingProgress;
    }

    public bool Message(MessageIntent intent, MessageLevel level, params string[] messagePortions)
    {
      loggingProgress.Report(new MessageEventArgs(AppIntent[InterfaceIntent.IndexOf(intent)], AppLevel[InterfaceLevel.IndexOf(level)], messagePortions));
      return true;
    }

    public bool Message(MessageIntent intent, MessageLevel level, Exception ex, params string[] messagePortions)
    {
      loggingProgress.Report(new MessageEventArgs(AppIntent[InterfaceIntent.IndexOf(intent)], AppLevel[InterfaceLevel.IndexOf(level)], ex, messagePortions));
      return true;
    }
  }
}
