using System;
using System.Threading.Tasks;
using Speckle.Core.Logging;

namespace Speckle.Core.Serialisation.SerializationUtilities;

internal enum WorkerThreadTaskType
{
  NoOp = default,
  Deserialize,
}

internal sealed class DeserializationWorkerThreads : ParallelOperationExecutor<WorkerThreadTaskType>
{
  private int _freeThreadCount;

  private readonly object _lockFreeThreads = new();
  private readonly BaseObjectDeserializerV2 _serializer;

  public DeserializationWorkerThreads(BaseObjectDeserializerV2 serializer, int threadCount)
  {
    _serializer = serializer;
    NumThreads = threadCount;
  }

  public override void Dispose()
  {
    lock (_lockFreeThreads)
    {
      _freeThreadCount -= NumThreads;
    }

    base.Dispose();
  }

  protected override void ThreadMain()
  {
    while (true)
    {
      lock (_lockFreeThreads)
      {
        _freeThreadCount++;
      }

      var (taskType, inputValue, tcs) = Tasks.Take();
      if (taskType == WorkerThreadTaskType.NoOp || tcs == null)
      {
        return;
      }

      try
      {
        var result = RunOperation(taskType, inputValue!, _serializer);
        tcs.SetResult(result);
      }
      catch (Exception ex)
      {
        tcs.SetException(ex);

        if (ex.IsFatal())
        {
          throw;
        }
      }
    }
  }

  private static object? RunOperation(
    WorkerThreadTaskType taskType,
    object inputValue,
    BaseObjectDeserializerV2 serializer
  )
  {
    switch (taskType)
    {
      case WorkerThreadTaskType.Deserialize:
        var converted = serializer.DeserializeTransportObject((string)inputValue);
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
    lock (_lockFreeThreads)
    {
      if (_freeThreadCount > 0)
      {
        canStartTask = true;
        _freeThreadCount--;
      }
    }

    if (!canStartTask)
    {
      return null;
    }

    TaskCompletionSource<object?> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Tasks.Add(new(taskType, inputValue, tcs));
    return tcs.Task;
  }
}
