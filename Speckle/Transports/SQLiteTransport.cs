using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Speckle.Transports
{
  // TODO: Investigate partitioning the object tables by the first two hash charaters. 
  public class SqlLiteObjectTransport : IDisposable, ITransport
  {
    public string TransportName { get; set; } = "Sqlite";

    public string RootPath { get; set; }
    public string ConnectionString { get; set; }

    private SQLiteConnection Connection { get; set; }

    private Dictionary<string, string> Buffer = new Dictionary<string, string>(100);
    private System.Timers.Timer WriteTimer;
    private int TotalElapsed = 0, PollInterval = 100;

    public SqlLiteObjectTransport(string basePath = null, string applicationName = "Speckle", string scope = "Objects")
    {
      if (basePath == null)
        basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

      RootPath = Path.Combine(basePath, applicationName, $"{scope}.db");
      ConnectionString = $@"URI=file:{RootPath}";

      InitializeTables();

      WriteTimer = new System.Timers.Timer() { AutoReset = false, Enabled = false, Interval = PollInterval };
      WriteTimer.Elapsed += WriteLocalBuffer;
      WriteTimer.Start();

    }

    private void InitializeTables()
    {
      Connection = new SQLiteConnection(ConnectionString);
      Connection.Open();
      using (var command = new SQLiteCommand(Connection))
      {
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS objects(
              hash TEXT PRIMARY KEY,
              content TEXT
            ) WITHOUT ROWID;
          ";

        command.ExecuteNonQuery();
      }
    }

    #region Writes
    private void WriteLocalBuffer(object sender, ElapsedEventArgs e)
    {
      TotalElapsed += PollInterval;
      if(Buffer.Count == 0)
      {
        // TODO: Investigate into emitting "completed" event? 
        return;
      }
      // If we don't have enough objects, or less than one second elapsed, exit
      if (Buffer.Count < 100 && TotalElapsed < 300)
      {
        //TotalElapsed = 0;
        WriteTimer.Start();
        return;
      }

      lock (Buffer)
      {
        Console.WriteLine($"writing {Buffer.Count} objs");
        TotalElapsed = 0;
        using (var t = Connection.BeginTransaction())
        {
          using (var command = new SQLiteCommand(Connection))
          {
            // TODO: bunch these up into bulk inserts of 100 objects?
            foreach (var kvp in Buffer)
            {
              command.CommandText = "INSERT OR IGNORE INTO objects(hash, content) VALUES(@hash, @content)";
              command.Parameters.AddWithValue("@hash", kvp.Key);
              command.Parameters.AddWithValue("@content", Utilities.CompressString(kvp.Value));
              command.ExecuteNonQuery();
            }
          }
          t.Commit();
          t.CommitAsync();
        }
        Buffer.Clear();
        WriteTimer.Start();
      }
    }

    /// <summary>
    /// Adds the object into a buffer that will be written to the db later.
    /// </summary>
    /// <param name="hash"></param>
    /// <param name="serializedObject"></param>
    /// <param name="overwrite"></param>
    public void SaveObject(string hash, string serializedObject, bool owerite = false)
    {
      lock (Buffer)
      {
        Buffer.Add(hash, serializedObject);
        WriteTimer.Start();
      }
    }

    /// <summary>
    /// Directly saves the object in the db.
    /// </summary>
    /// <param name="hash"></param>
    /// <param name="serializedObject"></param>
    public void SaveObjectSync(string hash, string serializedObject)
    {
      using (var command = new SQLiteCommand(Connection))
      {
        command.CommandText = "INSERT OR IGNORE INTO objects(hash, content) VALUES(@hash, @content)";
        command.Parameters.AddWithValue("@hash", hash);
        command.Parameters.AddWithValue("@content", Utilities.CompressString(serializedObject));
        command.ExecuteNonQuery();
      }
    }

    /// <summary>
    /// Directly saves the objects into the db.
    /// </summary>
    /// <param name="objects"></param>
    /// <returns></returns>
    public async Task SaveObjects(IEnumerable<(string, string)> objects)
    {
      using (var t = Connection.BeginTransaction())
      {
        using (var command = new SQLiteCommand(Connection))
        {
          foreach (var (hash, content) in objects)
          {
            command.CommandText = "INSERT OR IGNORE INTO objects(hash, content) VALUES(@hash, @content)";
            command.Parameters.AddWithValue("@hash", hash);
            command.Parameters.AddWithValue("@content", Utilities.CompressString(content));
            command.ExecuteNonQuery();
          }
        }
        await t.CommitAsync();
        return;
      }
    }

    #endregion

    #region Reads

    public string GetObject(string hash)
    {
      using (var command = new SQLiteCommand(Connection))
      {
        command.CommandText = "SELECT * FROM objects WHERE hash = @hash LIMIT 1 ";
        command.Parameters.AddWithValue("@hash", hash);
        var reader = command.ExecuteReader();
        while (reader.Read())
        {
          return Utilities.DecompressString(reader.GetString(1));
        }
        throw new Exception("No object found");
      }
    }

    public IEnumerable<string> GetObjects(IEnumerable<string> hashes)
    {
      //using (var command = new SQLiteCommand(Connection))
      //{
      //  command.CommandText = "SELECT * FROM objects WHERE hash = @hash LIMIT 1 ";
      //  command.Parameters.AddWithValue("@hash", hash);
      //  var reader = command.ExecuteReader();
      //  while (reader.Read())
      //  {
      //    yield return Utilities.DecompressString(reader.GetString(1));
      //  }
      //  throw new Exception("No object found");
      //}
      throw new NotImplementedException();
    }

    #endregion

    public void Dispose()
    {
      lock (Buffer) // wait for a lock on the buffer, in case the timer is now executing
      {
        Connection.Close();
        Connection.Dispose();
      }
    }
  }
}
