using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Data.Sqlite;
using Microsoft.Data.Sqlite;
using Speckle.Core.Api;

namespace Speckle.Core.Transports
{
  public class SQLiteTransport : IDisposable, ICloneable, ITransport
  {
    public string TransportName { get; set; } = "SQLite";

    public CancellationToken CancellationToken { get; set; }

    public string RootPath { get; set; }

    private string _BasePath { get; set; }
    private string _ApplicationName { get; set; }
    private string _Scope { get; set; }

    public string ConnectionString { get; set; }

    private SqliteConnection Connection { get; set; }
    private object ConnectionLock { get; set; }

    private ConcurrentQueue<(string, string, int)> Queue = new ConcurrentQueue<(string, string, int)>();

    public Action<string, int> OnProgressAction { get; set; }

    public Action<string, Exception> OnErrorAction { get; set; }
    public int SavedObjectCount { get; private set; }

    /// <summary>
    /// Timer that ensures queue is consumed if less than MAX_TRANSACTION_SIZE objects are being sent.
    /// </summary>
    private System.Timers.Timer WriteTimer;
    private int PollInterval = 500;

    private bool IS_WRITING = false;
    private int MAX_TRANSACTION_SIZE = 1000;

    public SQLiteTransport(string basePath = null, string applicationName = "Speckle", string scope = "Data")
    {
      if (basePath == null)
        basePath = Helpers.UserApplicationDataPath;
      _BasePath = basePath;

      if (applicationName == null)
        applicationName = "Speckle";
      _ApplicationName = applicationName;

      if (scope == null)
        scope = "Data";
      _Scope = scope;

      var dir = Path.Combine(basePath, applicationName);
      try
      {
        Directory.CreateDirectory(dir); //ensure dir is there
      }
      catch (Exception ex)
      {
        throw new Exception($"Cound not create {dir}", ex);
      }


      RootPath = Path.Combine(basePath, applicationName, $"{scope}.db");
      //fix for network drives: https://stackoverflow.com/a/18506097/826060
      var prefix = RootPath.StartsWith(@"\\") ? @"\\" : "";
      ConnectionString = string.Format("Data Source={0};", prefix + RootPath);

      try
      {
        Initialize();

        WriteTimer = new System.Timers.Timer() { AutoReset = true, Enabled = false, Interval = PollInterval };
        WriteTimer.Elapsed += WriteTimerElapsed;
      }
      catch (Exception e)
      {
        OnErrorAction?.Invoke(TransportName, e);
      }
    }

    private void Initialize()
    {

      // NOTE: used for creating partioned object tables.
      //string[] HexChars = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f" };
      //var cart = new List<string>();
      //foreach (var str in HexChars)
      //  foreach (var str2 in HexChars)
      //    cart.Add(str + str2);
      if (CancellationToken.IsCancellationRequested) return;

      using (var c = new SqliteConnection(ConnectionString))
      {
        c.Open();
        var commandText = @"
            CREATE TABLE IF NOT EXISTS objects(
              hash TEXT PRIMARY KEY,
              content TEXT
            ) WITHOUT ROWID;
          ";
        using (var command = new SqliteCommand(commandText, c))
        {
          command.ExecuteNonQuery();
        }

        // Insert Optimisations

        SqliteCommand cmd;
        cmd = new SqliteCommand("PRAGMA journal_mode='wal';", c);
        cmd.ExecuteNonQuery();

        //Note / Hack: This setting has the potential to corrupt the db.
        //cmd = new SqliteCommand("PRAGMA synchronous=OFF;", Connection);
        //cmd.ExecuteNonQuery();

        cmd = new SqliteCommand("PRAGMA count_changes=OFF;", c);
        cmd.ExecuteNonQuery();

        cmd = new SqliteCommand("PRAGMA temp_store=MEMORY;", c);
        cmd.ExecuteNonQuery();
      }

      Connection = new SqliteConnection(ConnectionString);
      Connection.Open();
      ConnectionLock = new object();

      if (CancellationToken.IsCancellationRequested) return;
    }

    public void BeginWrite()
    {
      Queue = new ConcurrentQueue<(string, string, int)>();
      SavedObjectCount = 0;
    }

    public void EndWrite() { }

    #region Writes

    /// <summary>
    /// Awaits untill write completion (ie, the current queue is fully consumed).
    /// </summary>
    /// <returns></returns>
    public async Task WriteComplete()
    {
      await Utilities.WaitUntil(() => { return GetWriteCompletionStatus(); }, 500);
    }

    /// <summary>
    /// Returns true if the current write queue is empty and comitted.
    /// </summary>
    /// <returns></returns>
    public bool GetWriteCompletionStatus()
    {
      Console.WriteLine($"write completion {Queue.Count == 0 && !IS_WRITING}");
      return Queue.Count == 0 && !IS_WRITING;
    }

    private void WriteTimerElapsed(object sender, ElapsedEventArgs e)
    {
      WriteTimer.Enabled = false;

      if (CancellationToken.IsCancellationRequested)
      {
        Queue = new ConcurrentQueue<(string, string, int)>();
        return;
      }

      if (!IS_WRITING && Queue.Count != 0)
        ConsumeQueue();
    }

    private void ConsumeQueue()
    {
      if (CancellationToken.IsCancellationRequested)
      {
        Queue = new ConcurrentQueue<(string, string, int)>();
        return;
      }

      IS_WRITING = true;
      var i = 0;
      ValueTuple<string, string, int> result;

      var saved = 0;

      using (var c = new SqliteConnection(ConnectionString))
      {
        c.Open();
        using (var t = c.BeginTransaction())
        {
          var commandText = $"INSERT OR IGNORE INTO objects(hash, content) VALUES(@hash, @content)";

          while (i < MAX_TRANSACTION_SIZE && Queue.TryPeek(out result))
          {
            using (var command = new SqliteCommand(commandText, c, t))
            {

              Queue.TryDequeue(out result);
              command.Parameters.AddWithValue("@hash", result.Item1);
              command.Parameters.AddWithValue("@content", result.Item2);
              command.ExecuteNonQuery();

              saved++;
            }
          }
          t.Commit();
          if (CancellationToken.IsCancellationRequested)
          {
            Queue = new ConcurrentQueue<(string, string, int)>();
            IS_WRITING = false;
            return;
          }

        }
      }

      if (OnProgressAction != null)
        OnProgressAction(TransportName, saved);

      if (CancellationToken.IsCancellationRequested)
      {
        Queue = new ConcurrentQueue<(string, string, int)>();
        IS_WRITING = false;
        return;
      }

      if (Queue.Count > 0)
        ConsumeQueue();

      IS_WRITING = false;
    }

    /// <summary>
    /// Adds an object to the saving queue. 
    /// </summary>
    /// <param name="hash"></param>
    /// <param name="serializedObject"></param>
    public void SaveObject(string hash, string serializedObject)
    {
      Queue.Enqueue((hash, serializedObject, System.Text.Encoding.UTF8.GetByteCount(serializedObject)));

      WriteTimer.Enabled = true;
      WriteTimer.Start();
    }

    public void SaveObject(string hash, ITransport sourceTransport)
    {
      var serializedObject = sourceTransport.GetObject(hash);
      Queue.Enqueue((hash, serializedObject, System.Text.Encoding.UTF8.GetByteCount(serializedObject)));
    }

    /// <summary>
    /// Directly saves the object in the db.
    /// </summary>
    /// <param name="hash"></param>
    /// <param name="serializedObject"></param>
    public void SaveObjectSync(string hash, string serializedObject)
    {
      try
      {
        using (var c = new SqliteConnection(ConnectionString))
        {
          c.Open();
          var commandText = $"INSERT OR IGNORE INTO objects(hash, content) VALUES(@hash, @content)";
          using (var command = new SqliteCommand(commandText, c))
          {

            command.Parameters.AddWithValue("@hash", hash);
            command.Parameters.AddWithValue("@content", serializedObject);
            command.ExecuteNonQuery();
          }
        }
      }
      catch (Exception e)
      {
        OnErrorAction?.Invoke(TransportName, e);
      }
    }

    #endregion

    #region Reads

    /// <summary>
    /// Gets an object.
    /// </summary>
    /// <param name="hash"></param>
    /// <returns></returns>
    public string GetObject(string hash)
    {
      if (CancellationToken.IsCancellationRequested) return null;
      lock (ConnectionLock)
      {
        using (var command = new SqliteCommand("SELECT * FROM objects WHERE hash = @hash LIMIT 1 ", Connection))
        {
          command.Parameters.AddWithValue("@hash", hash);
          using (var reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              if (CancellationToken.IsCancellationRequested) return null;
              return reader.GetString(1);
            }
          }
        }
      }
      return null; // pass on the duty of null checks to consumers
    }

    public async Task<string> CopyObjectAndChildren(string hash, ITransport targetTransport, Action<int> onTotalChildrenCountKnown = null)
    {
      throw new NotImplementedException();
    }

    #endregion

    /// <summary>
    /// Returns all the objects in the store. Note: do not use for large collections.
    /// </summary>
    /// <returns></returns>
    internal IEnumerable<string> GetAllObjects()
    {
      if (CancellationToken.IsCancellationRequested) yield break; // Check for cancellation

      using var c = new SqliteConnection(ConnectionString);
      c.Open();

      using var command = new SqliteCommand("SELECT * FROM objects", c);

      using var reader = command.ExecuteReader();
      while (reader.Read())
      {
        if (CancellationToken.IsCancellationRequested) yield break; // Check for cancellation
        yield return reader.GetString(1);
      }
    }

    /// <summary>
    /// Deletes an object. Note: do not use for any speckle object transport, as it will corrupt the database.
    /// </summary>
    /// <param name="hash"></param>
    public void DeleteObject(string hash)
    {
      if (CancellationToken.IsCancellationRequested) return;

      using (var c = new SqliteConnection(ConnectionString))
      {
        c.Open();
        using (var command = new SqliteCommand("DELETE FROM objects WHERE hash = @hash", c))
        {
          command.Parameters.AddWithValue("@hash", hash);
          command.ExecuteNonQuery();
        }
      }
    }

    /// <summary>
    /// Updates an object.
    /// </summary>
    /// <param name="hash"></param>
    /// <param name="serializedObject"></param>
    public void UpdateObject(string hash, string serializedObject)
    {
      if (CancellationToken.IsCancellationRequested) return;

      using (var c = new SqliteConnection(ConnectionString))
      {
        c.Open();
        var commandText = $"REPLACE INTO objects(hash, content) VALUES(@hash, @content)";
        using (var command = new SqliteCommand(commandText, c))
        {
          command.Parameters.AddWithValue("@hash", hash);
          command.Parameters.AddWithValue("@content", serializedObject);
          command.ExecuteNonQuery();
        }
      }
    }

    public override string ToString()
    {
      return $"Sqlite Transport @{RootPath}";
    }

    public async Task<Dictionary<string, bool>> HasObjects(List<string> objectIds)
    {
      Dictionary<string, bool> ret = new Dictionary<string, bool>();
      // Initialize with false so that canceled queries still return a dictionary item for every object id
      foreach (string objectId in objectIds) ret[objectId] = false;

      using (var c = new SqliteConnection(ConnectionString))
      {
        c.Open();
        foreach (string objectId in objectIds)
        {
          if (CancellationToken.IsCancellationRequested) return ret;
          var commandText = "SELECT 1 FROM objects WHERE hash = @hash LIMIT 1 ";
          using (var command = new SqliteCommand(commandText, c))
          {
            command.Parameters.AddWithValue("@hash", objectId);
            using (var reader = command.ExecuteReader())
            {
              bool rowFound = reader.Read();
              ret[objectId] = rowFound;
            }
          }
        }
      }
      return ret;
    }

    public void Dispose()
    {
      // TODO: Check if it's still writing?
      Connection?.Close();
      Connection?.Dispose();
      WriteTimer.Dispose();
    }

    public object Clone()
    {
      return new SQLiteTransport(_BasePath, _ApplicationName, _Scope) { OnProgressAction = OnProgressAction, OnErrorAction = OnErrorAction, CancellationToken = CancellationToken };
    }
  }
}
