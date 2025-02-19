using System;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.UI;

namespace RevitSharedResources.Models;

/// <summary>
/// This class gives access to the Revit API context from anywhere in your codebase. This is essentially a
/// lite version of the Revit.Async package from Kennan Chan. Most of the functionality was taken from that code.
/// The main difference is that this class does not subscribe to the applicationIdling event from revit
/// which the docs say will impact the performance of Revit
/// </summary>
public static class APIContext
{
  private static SemaphoreSlim semaphore = new(1, 1);
  private static UIControlledApplication uiApplication;
  private static ExternalEventHandler<IExternalEventHandler, ExternalEvent> factoryExternalEventHandler;
  private static ExternalEvent factoryExternalEvent;

  /// <summary>
  /// Initialize that happens in a Revit context will make an ExternalEvent and ExternalEventHandler
  /// whose jobs are to create external events that wrap the funcs passed into the Run method.
  /// </summary>
  /// <param name="application"></param>
  public static void Initialize(UIControlledApplication application)
  {
    uiApplication = application;
    factoryExternalEventHandler = new(ExternalEvent.Create);
    factoryExternalEvent = ExternalEvent.Create(factoryExternalEventHandler);
  }

  public static async Task<TResult> Run<TResult>(Func<UIControlledApplication, TResult> func)
  {
    await semaphore.WaitAsync().ConfigureAwait(false);
    try
    {
      var handler = new ExternalEventHandler<UIControlledApplication, TResult>(func);
      using var externalEvent = await Run(factoryExternalEventHandler, handler, factoryExternalEvent)
        .ConfigureAwait(false);

      return await Run(handler, uiApplication, externalEvent).ConfigureAwait(false);
    }
    finally
    {
      semaphore.Release();
    }
  }

  public static async Task Run(Action<UIControlledApplication> action)
  {
    await Run<object>(app =>
      {
        action(app);
        return null;
      })
      .ConfigureAwait(false);
  }

  public static async Task Run(Action action)
  {
    await Run<object>(_ =>
      {
        action();
        return null;
      })
      .ConfigureAwait(false);
  }

  private static async Task<TResult> Run<TParameter, TResult>(
    ExternalEventHandler<TParameter, TResult> handler,
    TParameter parameter,
    ExternalEvent externalEvent
  )
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

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1031:Do not catch general exception types",
    Justification = "This is a very generic utility method for running things in a Revit context. If the result of the Run method is awaited, then the exception caught here will be raised there."
  )]
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
