#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Data.Sqlite;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Timer = System.Timers.Timer;

namespace Speckle.Core.Transports;

public sealed class SQLiteTransport : IDisposable, ICloneable, ITransport, IBlobCapableTransport
{
  private bool _isWriting;
  private const int MaxTransactionSize = 1000;
  private const int PollInterval = 500;

  private ConcurrentQueue<(string, string, int)> _queue = new();

  /// <summary>
  /// Timer that ensures queue is consumed if less than MAX_TRANSACTION_SIZE objects are being sent.
  /// </summary>
  private readonly Timer _writeTimer;

  public SQLiteTransport(string? basePath = null, string? applicationName = null, string? scope = null)
  {
    _basePath = basePath ?? SpecklePathProvider.UserApplicationDataPath();
    _applicationName = applicationName ?? "Speckle";
    _scope = scope ?? "Data";

    var dir = Path.Combine(_basePath, _applicationName);
    try
    {
      Directory.CreateDirectory(dir); //ensure dir is there
    }
    catch (Exception ex)
    {
      throw new TransportException(this, $"Could not create {dir}", ex);
    }

    _rootPath = Path.Combine(_basePath, _applicationName, $"{_scope}.db");
    _connectionString = $"Data Source={_rootPath};";

    try
    {
      Initialize();

      _writeTimer = new Timer
      {
        AutoReset = true,
        Enabled = false,
        Interval = PollInterval
      };
      _writeTimer.Elapsed += WriteTimerElapsed;
    }
    catch (Exception ex)
    {
      throw new TransportException(this, "Failed to initialize DB connection", ex);
    }
  }

  private readonly string _rootPath;

  private readonly string _basePath;
  private readonly string _applicationName;
  private readonly string _scope;
  private readonly string _connectionString;

  private SqliteConnection Connection { get; set; }
  private object ConnectionLock { get; set; }

  public string BlobStorageFolder => SpecklePathProvider.BlobStoragePath(Path.Combine(_basePath, _applicationName));

  public void SaveBlob(Blob obj)
  {
    var blobPath = obj.originalPath;
    var targetPath = obj.getLocalDestinationPath(BlobStorageFolder);
    File.Copy(blobPath, targetPath, true);
  }

  public object Clone()
  {
    return new SQLiteTransport(_basePath, _applicationName, _scope)
    {
      OnProgressAction = OnProgressAction,
      OnErrorAction = OnErrorAction,
      CancellationToken = CancellationToken
    };
  }

  public void Dispose()
  {
    // TODO: Check if it's still writing?
    Connection.Close();
    Connection.Dispose();
    _writeTimer.Dispose();
  }

  public string TransportName { get; set; } = "SQLite";

  public Dictionary<string, object> TransportContext =>
    new()
    {
      { "name", TransportName },
      { "type", GetType().Name },
      { "basePath", _basePath },
      { "applicationName", _applicationName },
      { "scope", _scope },
      { "blobStorageFolder", BlobStorageFolder }
    };

  public CancellationToken CancellationToken { get; set; }

  public Action<string, int>? OnProgressAction { get; set; }

  [Obsolete("Transports will now throw exceptions")]
  public Action<string, Exception>? OnErrorAction { get; set; }
  public int SavedObjectCount { get; private set; }

  public TimeSpan Elapsed { get; private set; }

  public void BeginWrite()
  {
    _queue = new ConcurrentQueue<(string, string, int)>();
    SavedObjectCount = 0;
  }

  public void EndWrite() { }

  public async Task<Dictionary<string, bool>> HasObjects(IReadOnlyList<string> objectIds)
  {
    Dictionary<string, bool> ret = new(objectIds.Count);
    // Initialize with false so that canceled queries still return a dictionary item for every object id
    foreach (string objectId in objectIds)
      ret[objectId] = false;

    using var c = new SqliteConnection(_connectionString);
    c.Open();

    foreach (string objectId in objectIds)
    {
      CancellationToken.ThrowIfCancellationRequested();
      const string commandText = "SELECT 1 FROM objects WHERE hash = @hash LIMIT 1 ";
      using var command = new SqliteCommand(commandText, c);
      command.Parameters.AddWithValue("@hash", objectId);
      using var reader = command.ExecuteReader();
      bool rowFound = reader.Read();
      ret[objectId] = rowFound;
    }

    return ret;
  }

  private void Initialize()
  {
    // NOTE: used for creating partioned object tables.
    //string[] HexChars = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f" };
    //var cart = new List<string>();
    //foreach (var str in HexChars)
    //  foreach (var str2 in HexChars)
    //    cart.Add(str + str2);
    CancellationToken.ThrowIfCancellationRequested();

    using (var c = new SqliteConnection(_connectionString))
    {
      c.Open();
      const string commandText =
        @"
            CREATE TABLE IF NOT EXISTS objects(
              hash TEXT PRIMARY KEY,
              content TEXT
            ) WITHOUT ROWID;
          ";
      using (var command = new SqliteCommand(commandText, c))
        command.ExecuteNonQuery();

      // Insert Optimisations

      using SqliteCommand cmd0 = new("PRAGMA journal_mode='wal';", c);
      cmd0.ExecuteNonQuery();

      //Note / Hack: This setting has the potential to corrupt the db.
      //cmd = new SqliteCommand("PRAGMA synchronous=OFF;", Connection);
      //cmd.ExecuteNonQuery();

      using SqliteCommand cmd1 = new("PRAGMA count_changes=OFF;", c);
      cmd1.ExecuteNonQuery();

      using SqliteCommand cmd2 = new("PRAGMA temp_store=MEMORY;", c);
      cmd2.ExecuteNonQuery();
    }

    Connection = new SqliteConnection(_connectionString);
    Connection.Open();
    ConnectionLock = new object();
  }

  /// <summary>
  /// Returns all the objects in the store. Note: do not use for large collections.
  /// </summary>
  /// <returns></returns>
  internal IEnumerable<string> GetAllObjects()
  {
    CancellationToken.ThrowIfCancellationRequested();

    using var c = new SqliteConnection(_connectionString);
    c.Open();

    using var command = new SqliteCommand("SELECT * FROM objects", c);

    using var reader = command.ExecuteReader();
    while (reader.Read())
    {
      CancellationToken.ThrowIfCancellationRequested();
      yield return reader.GetString(1);
    }
  }

  /// <summary>
  /// Deletes an object. Note: do not use for any speckle object transport, as it will corrupt the database.
  /// </summary>
  /// <param name="hash"></param>
  public void DeleteObject(string hash)
  {
    CancellationToken.ThrowIfCancellationRequested();

    using var c = new SqliteConnection(_connectionString);
    c.Open();
    using var command = new SqliteCommand("DELETE FROM objects WHERE hash = @hash", c);
    command.Parameters.AddWithValue("@hash", hash);
    command.ExecuteNonQuery();
  }

  /// <summary>
  /// Updates an object.
  /// </summary>
  /// <param name="hash"></param>
  /// <param name="serializedObject"></param>
  public void UpdateObject(string hash, string serializedObject)
  {
    CancellationToken.ThrowIfCancellationRequested();

    using var c = new SqliteConnection(_connectionString);
    c.Open();
    const string commandText = "REPLACE INTO objects(hash, content) VALUES(@hash, @content)";
    using var command = new SqliteCommand(commandText, c);
    command.Parameters.AddWithValue("@hash", hash);
    command.Parameters.AddWithValue("@content", serializedObject);
    command.ExecuteNonQuery();
  }

  public override string ToString()
  {
    return $"Sqlite Transport @{_rootPath}";
  }

  #region Writes

  /// <summary>
  /// Awaits untill write completion (ie, the current queue is fully consumed).
  /// </summary>
  /// <returns></returns>
  public async Task WriteComplete()
  {
    await Utilities
      .WaitUntil(
        () =>
        {
          return GetWriteCompletionStatus();
        },
        500
      )
      .ConfigureAwait(false);
  }

  /// <summary>
  /// Returns true if the current write queue is empty and comitted.
  /// </summary>
  /// <returns></returns>
  public bool GetWriteCompletionStatus()
  {
    return _queue.IsEmpty && !_isWriting;
  }

  private void WriteTimerElapsed(object sender, ElapsedEventArgs e)
  {
    _writeTimer.Enabled = false;

    if (CancellationToken.IsCancellationRequested)
    {
      _queue = new ConcurrentQueue<(string, string, int)>();
      return;
    }

    if (!_isWriting && !_queue.IsEmpty)
      ConsumeQueue();
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

      using (var c = new SqliteConnection(_connectionString))
      {
        c.Open();
        using var t = c.BeginTransaction();
        const string commandText = "INSERT OR IGNORE INTO objects(hash, content) VALUES(@hash, @content)";

        while (i < MaxTransactionSize && _queue.TryPeek(out (string id, string serializedObject, int byteCount) result))
        {
          using var command = new SqliteCommand(commandText, c, t);
          _queue.TryDequeue(out result);
          command.Parameters.AddWithValue("@hash", result.id);
          command.Parameters.AddWithValue("@content", result.serializedObject);
          command.ExecuteNonQuery();

          saved++;
        }

        t.Commit();
        CancellationToken.ThrowIfCancellationRequested();
      }

      OnProgressAction?.Invoke(TransportName, saved);

      CancellationToken.ThrowIfCancellationRequested();

      if (!_queue.IsEmpty)
        ConsumeQueue();
    }
    catch (OperationCanceledException)
    {
      _queue = new ConcurrentQueue<(string, string, int)>();
    }
    finally
    {
      stopwatch.Stop();
      Elapsed += stopwatch.Elapsed;
      _isWriting = false;
    }
  }

  /// <summary>
  /// Adds an object to the saving queue.
  /// </summary>
  /// <param name="id"></param>
  /// <param name="serializedObject"></param>
  public void SaveObject(string id, string serializedObject)
  {
    _queue.Enqueue((id, serializedObject, Encoding.UTF8.GetByteCount(serializedObject)));

    _writeTimer.Enabled = true;
    _writeTimer.Start();
  }

  public void SaveObject(string id, ITransport sourceTransport)
  {
    CancellationToken.ThrowIfCancellationRequested();

    var serializedObject = sourceTransport.GetObject(id);

    if (serializedObject is null)
      throw new TransportException(
        this,
        $"Cannot copy {id} from {sourceTransport.TransportName} to {TransportName} as source returned null"
      );

    //Should this just call SaveObject... do we not want the write timers?
    _queue.Enqueue((id, serializedObject, Encoding.UTF8.GetByteCount(serializedObject)));
  }

  /// <summary>
  /// Directly saves the object in the db.
  /// </summary>
  /// <param name="hash"></param>
  /// <param name="serializedObject"></param>
  public void SaveObjectSync(string hash, string serializedObject)
  {
    const string commandText = "INSERT OR IGNORE INTO objects(hash, content) VALUES(@hash, @content)";

    using var c = new SqliteConnection(_connectionString);
    c.Open();

    using var command = new SqliteCommand(commandText, c);
    command.Parameters.AddWithValue("@hash", hash);
    command.Parameters.AddWithValue("@content", serializedObject);
    command.ExecuteNonQuery();
  }

  #endregion

  #region Reads

  /// <summary>
  /// Gets an object.
  /// </summary>
  /// <param name="id"></param>
  /// <returns></returns>
  public string? GetObject(string id)
  {
    CancellationToken.ThrowIfCancellationRequested();
    lock (ConnectionLock)
    {
      var startTime = Stopwatch.GetTimestamp();
      using (var command = new SqliteCommand("SELECT * FROM objects WHERE hash = @hash LIMIT 1 ", Connection))
      {
        command.Parameters.AddWithValue("@hash", id);
        using (var reader = command.ExecuteReader())
          while (reader.Read())
          {
            return reader.GetString(1);
          }
      }
      Elapsed += LoggingHelpers.GetElapsedTime(startTime, Stopwatch.GetTimestamp());
    }
    return null; // pass on the duty of null checks to consumers
  }

  public async Task<string> CopyObjectAndChildren(
    string id,
    ITransport targetTransport,
    Action<int>? onTotalChildrenCountKnown = null
  )
  {
    throw new NotImplementedException();
  }

  #endregion
}
