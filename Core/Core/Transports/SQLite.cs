using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.Core.Transports;

public sealed class SQLiteTransport : BatchingWriteTransport, ITransport, IBlobCapableTransport
{

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

  protected override void Dispose(bool disposing)
  {
    base.Dispose(disposing);
    if (disposing)
    {
      // TODO: Check if it's still writing?
      Connection.Close();
      Connection.Dispose();
    }
  }

  public override string TransportName { get; set; } = "SQLite";

  public override Dictionary<string, object> TransportContext =>
    new()
    {
      { "name", TransportName },
      { "type", GetType().Name },
      { "basePath", _basePath },
      { "applicationName", _applicationName },
      { "scope", _scope },
      { "blobStorageFolder", BlobStorageFolder }
    };



  public override Task<Dictionary<string, bool>> HasObjects(IReadOnlyList<string> objectIds)
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
  /// <remarks>This function uses a separate <see cref="SqliteConnection"/> so is safe to call concurrently (unlike most other transport functions)</remarks>
  internal IEnumerable<string> GetAllObjects()
  {
    CancellationToken.ThrowIfCancellationRequested();

    using SqliteConnection connection = new(_connectionString);
    connection.Open();

    using var command = new SqliteCommand("SELECT * FROM objects", connection);

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
  public override void DeleteObject(string hash)
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
  public override void UpdateObject(string hash, string serializedObject)
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
  /// Directly saves the object in the db.
  /// </summary>
  /// <param name="hash"></param>
  /// <param name="serializedObject"></param>
  public override void SaveObjectSync(string hash, string serializedObject)
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

  protected override void WriteBatch(List<WriteItem> batch)
  {
    using var c = new SqliteConnection(_connectionString);
    c.Open();
    using var t = c.BeginTransaction();
    const string COMMAND_TEXT = "INSERT OR IGNORE INTO objects(hash, content) VALUES(@hash, @content)";

    foreach(var item in batch)
    {
      using var command = new SqliteCommand(COMMAND_TEXT, c, t);
      command.Parameters.AddWithValue("@hash", item.Id);
      command.Parameters.AddWithValue("@content", item.SerializedObject);
      command.ExecuteNonQuery();
    }

    t.Commit();
    CancellationToken.ThrowIfCancellationRequested();
  }
  
  #endregion

  #region Reads

  /// <summary>
  /// Gets an object.
  /// </summary>
  /// <param name="id"></param>
  /// <returns></returns>
  public override string? GetObject(string id)
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

  #endregion
}
