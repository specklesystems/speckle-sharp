#nullable enable
using System;
using System.Threading.Tasks;

namespace Speckle.Core.Serialisation;

internal enum WorkerThreadTaskType
{
  _NoOp = default,
  Deserialize,
}

internal class DeserializationWorkerThreads : ParallelOperationExecutor<WorkerThreadTaskType>
{
  private int FreeThreadCount;

  private object LockFreeThreads = new();
  private BaseObjectDeserializerV2 Serializer;

  public DeserializationWorkerThreads(BaseObjectDeserializerV2 serializer)
  {
    Serializer = serializer;
    this.NumThreads = Environment.ProcessorCount;
  }

  public override void Dispose()
  {
    lock (LockFreeThreads)
      FreeThreadCount -= NumThreads;
    base.Dispose();
  }

  protected override void ThreadMain()
  {
    while (true)
    {
      lock (LockFreeThreads)
        FreeThreadCount++;
      var (taskType, inputValue, tcs) = Tasks.Take();
      if (taskType == WorkerThreadTaskType._NoOp || tcs == null)
        return;

      try
      {
        var result = RunOperation(taskType, inputValue!, Serializer);
        tcs.SetResult(result);
      }
      catch (Exception ex)
      {
        tcs.SetException(ex);
      }
    }
  }

  private static object RunOperation(
    WorkerThreadTaskType taskType,
    object inputValue,
    BaseObjectDeserializerV2 serializer
  )
  {
    switch (taskType)
    {
      case WorkerThreadTaskType.Deserialize:
        var converted = serializer.DeserializeTransportObject(inputValue as string);
        return converted;
      default:
        throw new ArgumentException(
          $"No implementation for {nameof(WorkerThreadTaskType)} with value {taskType}",
          nameof(taskType)
        );
    }
  }

  internal Task<object?>? TryStartTask(WorkerThreadTaskType taskType, object inputValue)
  {
    bool canStartTask = false;
    lock (LockFreeThreads)
    {
      if (FreeThreadCount > 0)
      {
        canStartTask = true;
        FreeThreadCount--;
      }
    }

    if (!canStartTask)
      return null;

    TaskCompletionSource<object?> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Tasks.Add(new(taskType, inputValue, tcs));
    return tcs.Task;
  }
}
