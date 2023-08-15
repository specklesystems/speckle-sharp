#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Speckle.Core.Serialisation;

internal readonly struct OperationTask<T> where T : struct
{
  public readonly T OperationType;
  public readonly object? InputValue;
  public readonly TaskCompletionSource<object?>? Tcs;

  public OperationTask(T operationType, object? inputValue = null, TaskCompletionSource<object?>? tcs = null)
  {
    OperationType = operationType;
    InputValue = inputValue;
    Tcs = tcs;
  }

  public void Deconstruct(out T operationType, out object? inputValue, out TaskCompletionSource<object?>? tcs)
  {
    operationType = OperationType;
    inputValue = InputValue;
    tcs = Tcs;
  }
}

internal abstract class ParallelOperationExecutor<TOperation> : IDisposable where TOperation : struct
{
  protected BlockingCollection<OperationTask<TOperation>> Tasks { get; set; } = new();

  protected IList<Thread> Threads { get; set; } = new List<Thread>();

  public int NumThreads { get; protected set; }
  public bool HasStarted => Threads.Count > 0;

  protected abstract void ThreadMain();

  protected virtual void Stop()
  {
    if (!HasStarted)
      throw new InvalidOperationException($"Unable to {nameof(Stop)} {this} as it has not started!");
    foreach (Thread _ in Threads)
      Tasks.Add(default);
    foreach (Thread t in Threads)
      t.Join();
    Threads = new List<Thread>();
  }

  public virtual void Start()
  {
    if (HasStarted)
      throw new InvalidOperationException($"{this}: Threads already started");
    for (int i = 0; i < NumThreads; i++)
    {
      Thread t = new(ThreadMain) { Name = ToString(), IsBackground = true };
      Threads.Add(t);
      t.Start();
    }
  }

  public virtual void Dispose()
  {
    if (HasStarted)
      Stop();
    Tasks.Dispose();
  }
}
