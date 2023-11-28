using System;
using Speckle.Core.Logging;

namespace Archicad.Helpers;

public class Timer : IDisposable
{
  internal sealed class Context : Speckle.Core.Helpers.State<Context>
  {
    public Speckle.Core.Logging.CumulativeTimer cumulativeTimer = null;
  }

  private SerilogTimings.Operation serilogOperation = null;

  public static Timer CreateReceive(string streamId)
  {
    return new Timer("Receive stream {streamId}", streamId);
  }

  public static Timer CreateSend(string streamId)
  {
    return new Timer("Send stream {streamId}", streamId);
  }

  public Timer(string messageTemplate, params object[] args)
  {
    Context context = Context.Push();
    serilogOperation = SerilogTimings.Operation.Begin(messageTemplate, args);
    context.cumulativeTimer = new CumulativeTimer();
  }

  public void Cancel()
  {
    serilogOperation.Cancel();
  }

  void IDisposable.Dispose()
  {
    Context context = Context.Peek;
    context?.cumulativeTimer?.EnrichSerilogOperation(serilogOperation);
    serilogOperation.Complete();
  }
}
