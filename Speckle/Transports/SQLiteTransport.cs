using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Timers;

namespace Speckle.Transports
{
  // TODO: Investigate partitioning the object tables by the first two hash charaters. 
  public class SqlLiteObjectTransport : ITransport, IDisposable
  {
    public string TransportName { get; set; } = "Sqlite";

    public string RootPath { get; set; }
    public string ConnectionString { get; set; }

    SQLiteConnection Connection { get; set; }

    Dictionary<string, string> Buffer = new Dictionary<string, string>(100);
    System.Timers.Timer writeTimer;
    int totalElapsed = 0, pollInterval = 100;

    public SqlLiteObjectTransport(string basePath = null, string applicationName = "Speckle", string scope = "Objects")
    {
      if (basePath == null)
        basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

      RootPath = Path.Combine(basePath, applicationName, $"{scope}.db");
      ConnectionString = $@"URI=file:{RootPath}";

      writeTimer = new System.Timers.Timer() { AutoReset = false, Enabled = false, Interval = pollInterval };
      writeTimer.Elapsed += WriteBuffer;
      writeTimer.Start();

      Initialize();
    }

    private void WriteBuffer(object sender, ElapsedEventArgs e)
    {
      totalElapsed += pollInterval;

      // If we don't have enough objects, or less than one second elapsed, exit
      if (Buffer.Count < 100 && totalElapsed < 300)
      {
        totalElapsed = 0;
        writeTimer.Start();
        return;
      }

      lock (Buffer)
      {
        totalElapsed = 0;
        using (var t = Connection.BeginTransaction())
        {
          using (var command = new SQLiteCommand(Connection))
          {
            // TODO: bunch these up into bulk inserts of 100 objects
            foreach (var kvp in Buffer)
            {
              command.CommandText = "INSERT OR IGNORE INTO objects(hash, content) VALUES(@hash, @content)";
              command.Parameters.AddWithValue("@hash", kvp.Key);
              command.Parameters.AddWithValue("@content", Utilities.CompressString(kvp.Value));
              command.ExecuteNonQuery();
            }
          }
          t.Commit();
        }
        Buffer.Clear();
        writeTimer.Start();
      }
    }

    public void Initialize()
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

    public void SaveObject(string hash, string serializedObject, bool overwrite = false)
    {
      lock (Buffer)
      {
        Buffer.Add(hash, serializedObject);
      }
    }

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
