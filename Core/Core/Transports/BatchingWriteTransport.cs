using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Data.Sqlite;
using Timer = System.Timers.Timer;

namespace Speckle.Core.Transports;

public abstract class BatchingWriteTransport : IDisposable, ITransport
{
  protected record struct WriteItem(string Id, string SerializedObject);
  
  private bool _isWriting;
  private const int MAX_TRANSACTION_SIZE = 1000;
  private const int POLL_INTERVAL = 500;

  private ConcurrentQueue<WriteItem> _queue = new();

  /// <summary>
  /// Timer that ensures queue is consumed if less than MAX_TRANSACTION_SIZE objects are being sent.
  /// </summary>
  private readonly Timer _writeTimer;

  protected BatchingWriteTransport()
  {
    _writeTimer = new Timer
    {
      AutoReset = true,
      Enabled = false,
      Interval = POLL_INTERVAL
    };
    _writeTimer.Elapsed += WriteTimerElapsed;
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing)
  {
    if (disposing)
    {
      _writeTimer.Dispose();
    }
  }
  
  public abstract string TransportName { get; set; }

  public abstract Dictionary<string, object> TransportContext { get; }

  public CancellationToken CancellationToken { get; set; }

  public Action<string, int>? OnProgressAction { get; set; }

  [Obsolete("Transports will now throw exceptions")]
  public Action<string, Exception>? OnErrorAction { get; set; }
  public int SavedObjectCount { get; private set; }

  public TimeSpan Elapsed { get; protected set; }

  public void BeginWrite()
  {
    _queue = new();
    SavedObjectCount = 0;
  }

  public void EndWrite() { }

  public abstract Task<Dictionary<string, bool>> HasObjects(IReadOnlyList<string> objectIds);


  /// <summary>
  /// Deletes an object. Note: do not use for any speckle object transport, as it will corrupt the database.
  /// </summary>
  /// <param name="hash"></param>
  public abstract void DeleteObject(string hash);

  /// <summary>
  /// Updates an object.
  /// </summary>
  /// <param name="hash"></param>
  /// <param name="serializedObject"></param>
  public abstract void UpdateObject(string hash, string serializedObject);


  #region Writes

  /// <summary>
  /// Awaits untill write completion (ie, the current queue is fully consumed).
  /// </summary>
  /// <returns></returns>
  public async Task WriteComplete()
  {
    await Utilities.WaitUntil(() => WriteCompletionStatus, 500).ConfigureAwait(false);
  }

  /// <summary>
  /// Returns true if the current write queue is empty and comitted.
  /// </summary>
  /// <returns></returns>
  public bool WriteCompletionStatus => _queue.IsEmpty && !_isWriting;

  private void WriteTimerElapsed(object sender, ElapsedEventArgs e)
  {
    _writeTimer.Enabled = false;

    if (CancellationToken.IsCancellationRequested)
    {
      _queue = new ConcurrentQueue<WriteItem>();
      return;
    }

    if (!_isWriting && !_queue.IsEmpty)
    {
      ConsumeQueue();
    }
  }

  private void ConsumeQueue()
  {
    var stopwatch = Stopwatch.StartNew();
    _isWriting = true;
    try
    {
      CancellationToken.ThrowIfCancellationRequested();

      var i = 0; //BUG: This never gets incremented!

      var saved = 0;

      var items = new List<WriteItem>();
        while (i < MAX_TRANSACTION_SIZE && _queue.TryPeek(out var result))
        {
          _queue.TryDequeue(out result);
          items.Add(result);
          saved++;
        }
        WriteBatch(items);

        CancellationToken.ThrowIfCancellationRequested();
      OnProgressAction?.Invoke(TransportName, saved);

      CancellationToken.ThrowIfCancellationRequested();

      if (!_queue.IsEmpty)
      {
        ConsumeQueue();
      }
    }
    catch (SqliteException ex)
    {
      throw new TransportException(this, "SQLite Command Failed", ex);
    }
    catch (OperationCanceledException)
    {
      _queue = new();
    }
    finally
    {
      stopwatch.Stop();
      Elapsed += stopwatch.Elapsed;
      _isWriting = false;
    }
  }

  protected abstract void WriteBatch(List<WriteItem> batch);

  /// <summary>
  /// Adds an object to the saving queue.
  /// </summary>
  /// <param name="id"></param>
  /// <param name="serializedObject"></param>
  public void SaveObject(string id, string serializedObject)
  {
    CancellationToken.ThrowIfCancellationRequested();
    _queue.Enqueue(new (id, serializedObject));

    _writeTimer.Enabled = true;
    _writeTimer.Start();
  }

  public void SaveObject(string id, ITransport sourceTransport)
  {
    CancellationToken.ThrowIfCancellationRequested();

    var serializedObject = sourceTransport.GetObject(id);

    if (serializedObject is null)
    {
      throw new TransportException(
        this,
        $"Cannot copy {id} from {sourceTransport.TransportName} to {TransportName} as source returned null"
      );
    }

    //Should this just call SaveObject... do we not want the write timers?
    _queue.Enqueue(new (id, serializedObject));
  }

  /// <summary>
  /// Directly saves the object in the db.
  /// </summary>
  /// <param name="hash"></param>
  /// <param name="serializedObject"></param>
  public abstract void SaveObjectSync(string hash, string serializedObject);

  #endregion

  #region Reads

  /// <summary>
  /// Gets an object.
  /// </summary>
  /// <param name="id"></param>
  /// <returns></returns>
  public abstract string? GetObject(string id);

  public Task<string> CopyObjectAndChildren(
    string id,
    ITransport targetTransport,
    Action<int>? onTotalChildrenCountKnown = null
  )
  {
    string res = TransportHelpers.CopyObjectAndChildrenSync(
      id,
      this,
      targetTransport,
      onTotalChildrenCountKnown,
      CancellationToken
    );
    return Task.FromResult(res);
  }

  #endregion

  #region Deprecated

  [Obsolete("Use " + nameof(WriteCompletionStatus))]
  [SuppressMessage("Design", "CA1024:Use properties where appropriate")]
  public bool GetWriteCompletionStatus() => WriteCompletionStatus;

  #endregion
}
