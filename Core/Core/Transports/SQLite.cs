using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
  private const int MAX_TRANSACTION_SIZE = 1000;
  private const int POLL_INTERVAL = 500;

  private ConcurrentQueue<(string id, string serializedObject, int byteCount)> _queue = new();

  /// <summary>
  /// Timer that ensures queue is consumed if less than MAX_TRANSACTION_SIZE objects are being sent.
  /// </summary>
  private readonly Timer _writeTimer;

  /// <summary>
  /// Connects to an SQLite DB at {<paramref name="basePath"/>}/{<paramref name="applicationName"/>}/{<paramref name="scope"/>}.db
  /// Will attempt to create db + directory structure as needed
  /// </summary>
  /// <param name="basePath">defaults to <see cref="SpecklePathProvider.UserApplicationDataPath"/> if <see langword="null"/></param>
  /// <param name="applicationName">defaults to <c>"Speckle"</c> if <see langword="null"/></param>
  /// <param name="scope">defaults to <c>"Data"</c> if <see langword="null"/></param>
  /// <exception cref="SqliteException">Failed to initialize a connection to the db</exception>
  /// <exception cref="TransportException">Path was invalid or could not be created</exception>
  public SQLiteTransport(string? basePath = null, string? applicationName = null, string? scope = null)
  {
    _basePath = basePath ?? SpecklePathProvider.UserApplicationDataPath();
    _applicationName = applicationName ?? "Speckle";
    _scope = scope ?? "Data";

    try
    {
      var dir = Path.Combine(_basePath, _applicationName);
      _rootPath = Path.Combine(dir, $"{_scope}.db");

      Directory.CreateDirectory(dir); //ensure dir is there
    }
    catch (Exception ex)
      when (ex is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
    {
      throw new TransportException($"Path was invalid or could not be created {_rootPath}", ex);
    }

    _connectionString = $"Data Source={_rootPath};";

    Initialize();

    _writeTimer = new Timer
    {
      AutoReset = true,
      Enabled = false,
      Interval = POLL_INTERVAL
    };
    _writeTimer.Elapsed += WriteTimerElapsed;
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
    var targetPath = obj.GetLocalDestinationPath(BlobStorageFolder);
    File.Copy(blobPath, targetPath, true);
  }

  public object Clone()
  {
    return new SQLiteTransport(_basePath, _applicationName, _scope)
    {
      OnProgressAction = OnProgressAction,
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
    _queue = new();
    SavedObjectCount = 0;
  }

  public void EndWrite() { }

  public Task<Dictionary<string, bool>> HasObjects(IReadOnlyList<string> objectIds)
  {
    Dictionary<string, bool> ret = new(objectIds.Count);
    // Initialize with false so that canceled queries still return a dictionary item for every object id
    foreach (string objectId in objectIds)
    {
      ret[objectId] = false;
    }

    try
    {
      const string COMMAND_TEXT = "SELECT 1 FROM objects WHERE hash = @hash LIMIT 1 ";
      using var command = new SqliteCommand(COMMAND_TEXT, Connection);

      foreach (string objectId in objectIds)
      {
        CancellationToken.ThrowIfCancellationRequested();

        command.Parameters.Clear();
        command.Parameters.AddWithValue("@hash", objectId);

        using var reader = command.ExecuteReader();
        bool rowFound = reader.Read();
        ret[objectId] = rowFound;
      }
    }
    catch (SqliteException ex)
    {
      throw new TransportException("SQLite transport failed", ex);
    }

    return Task.FromResult(ret);
  }

  /// <exception cref="SqliteException">Failed to initialize connection to the SQLite DB</exception>
  private void Initialize()
  {
    // NOTE: used for creating partioned object tables.
    //string[] HexChars = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f" };
    //var cart = new List<string>();
    //foreach (var str in HexChars)
    //  foreach (var str2 in HexChars)
    //    cart.Add(str + str2);

    using (var c = new SqliteConnection(_connectionString))
    {
      c.Open();
      const string COMMAND_TEXT =
        @"
            CREATE TABLE IF NOT EXISTS objects(
              hash TEXT PRIMARY KEY,
              content TEXT
            ) WITHOUT ROWID;
          ";
      using (var command = new SqliteCommand(COMMAND_TEXT, c))
      {
        command.ExecuteNonQuery();
      }

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

    using var command = new SqliteCommand("SELECT * FROM objects", Connection);

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

    using var command = new SqliteCommand("DELETE FROM objects WHERE hash = @hash", Connection);
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
    const string COMMAND_TEXT = "REPLACE INTO objects(hash, content) VALUES(@hash, @content)";
    using var command = new SqliteCommand(COMMAND_TEXT, c);
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
      _queue = new ConcurrentQueue<(string, string, int)>();
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

      using (var c = new SqliteConnection(_connectionString))
      {
        c.Open();
        using var t = c.BeginTransaction();
        const string COMMAND_TEXT = "INSERT OR IGNORE INTO objects(hash, content) VALUES(@hash, @content)";

        while (i < MAX_TRANSACTION_SIZE && _queue.TryPeek(out var result))
        {
          using var command = new SqliteCommand(COMMAND_TEXT, c, t);
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

  /// <summary>
  /// Adds an object to the saving queue.
  /// </summary>
  /// <param name="id"></param>
  /// <param name="serializedObject"></param>
  public void SaveObject(string id, string serializedObject)
  {
    CancellationToken.ThrowIfCancellationRequested();
    _queue.Enqueue((id, serializedObject, Encoding.UTF8.GetByteCount(serializedObject)));

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
    _queue.Enqueue((id, serializedObject, Encoding.UTF8.GetByteCount(serializedObject)));
  }

  /// <summary>
  /// Directly saves the object in the db.
  /// </summary>
  /// <param name="hash"></param>
  /// <param name="serializedObject"></param>
  public void SaveObjectSync(string hash, string serializedObject)
  {
    const string COMMAND_TEXT = "INSERT OR IGNORE INTO objects(hash, content) VALUES(@hash, @content)";

    try
    {
      using var command = new SqliteCommand(COMMAND_TEXT, Connection);
      command.Parameters.AddWithValue("@hash", hash);
      command.Parameters.AddWithValue("@content", serializedObject);
      command.ExecuteNonQuery();
    }
    catch (SqliteException ex)
    {
      throw new TransportException(this, "SQLite Command Failed", ex);
    }
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
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
          return reader.GetString(1);
        }
      }
      Elapsed += LoggingHelpers.GetElapsedTime(startTime, Stopwatch.GetTimestamp());
    }
    return null; // pass on the duty of null checks to consumers
  }

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
