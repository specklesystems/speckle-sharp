using System;
using Autodesk.Revit.UI;
using System.Threading.Tasks;
using System.Threading;

namespace RevitSharedResources.Models
{
  public static class APIContext
  {
    private static ExternalEventHandler<IExternalEventHandler, ExternalEvent> factoryExternalEventHandler;
    private static ExternalEvent factoryExternalEvent;
    public static void Initialize(UIControlledApplication application)
    {
      factoryExternalEventHandler = new(ExternalEvent.Create);
      factoryExternalEvent = ExternalEvent.Create(factoryExternalEventHandler);
    }

    public static async Task<TResult> Run<TResult>(Func<UIApplication, TResult> func)
    {
      var handler = new ExternalEventHandler<UIApplication, TResult>(func);
      using var externalEvent = await Run(factoryExternalEventHandler, handler, factoryExternalEvent)
        .ConfigureAwait(false);

      return await Run(handler, null, externalEvent).ConfigureAwait(false);
    }
    
    public static async Task Run(Action<UIApplication> action)
    {
      await Run<object>(app =>
      {
        action(app);
        return null;
      }).ConfigureAwait(false);
    }
    public static async Task Run(Action action)
    {
      await Run<object>(_ =>
      {
        action();
        return null;
      }).ConfigureAwait(false);
    }

    private static async Task<TResult> Run<TParameter, TResult>(
      ExternalEventHandler<TParameter, TResult> handler,
      TParameter parameter,
      ExternalEvent externalEvent)
    {
      var task = handler.GetTask(parameter);
      externalEvent.Raise();

      return await task.ConfigureAwait(false);
    }
  }

  public enum HandlerStatus
  {
    NotStarted,
    Started,
    IsCompleted,
    IsFaulted,
  }

  internal class ExternalEventHandler<TParameter, TResult> : IExternalEventHandler
  {
    public TaskCompletionSource<TResult> Result { get; private set; }
    public Task<TResult> GetTask(TParameter parameter)
    {
      Parameter = parameter;
      Result = new TaskCompletionSource<TResult>();
      return Result.Task;
    }

    private Func<TParameter, TResult> func;
    public ExternalEventHandler(Func<TParameter, TResult> func)
    {
      this.func = func;
    }

    public HandlerStatus Status { get; private set; } = HandlerStatus.NotStarted;
    public TParameter Parameter { get; private set; }
    public void Execute(UIApplication app)
    {
      Status = HandlerStatus.Started;
      try
      {
        var r = func(Parameter);
        Result.SetResult(r);
        Status = HandlerStatus.IsCompleted;
      }
      catch (Exception ex)
      {
        Status = HandlerStatus.IsFaulted;
        Result.SetException(ex);
      }
    }

    public string GetName() => "SpeckleRevitContextEventHandler";
  }
}
