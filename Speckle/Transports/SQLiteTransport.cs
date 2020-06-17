using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
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

    private Dictionary<string, string> Buffer = new Dictionary<string, string>();
    private System.Timers.Timer WriteTimer;
    private int TotalElapsed = 0, PollInterval = 100;
    private bool IsWriting = false;

    private int MAX_BUFFER_SIZE = 5000000; // 5 mb
    private int CURR_BUFFER_SIZE = 0;

    private string[] HexChars = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f" };

    public SqlLiteObjectTransport(string basePath = null, string applicationName = "Speckle", string scope = "Objects")
    {
      if (basePath == null)
        basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

      RootPath = Path.Combine(basePath, applicationName, $"{scope}.db");
      ConnectionString = $@"URI=file:{RootPath}; PRAGMA synchronous = OFF; PRAGMA journal_mode = MEMORY;";

      InitializeTables();

      WriteTimer = new System.Timers.Timer() { AutoReset = true, Enabled = false, Interval = PollInterval };
      WriteTimer.Elapsed += WriteTimerElapsed;
    }

    private void InitializeTables()
    {

      var cart = new List<string>();
      foreach (var str in HexChars)
        foreach (var str2 in HexChars)
          cart.Add(str + str2);

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

    public async Task WriteComplete()
    {
      await Utilities.WaitUntil(() => { return CURR_BUFFER_SIZE == 0; }, 100);
    }

    private void WriteTimerElapsed(object sender, ElapsedEventArgs e)
    {
      Console.WriteLine($"Write Timer Elapsed: {Buffer.Count} / {CURR_BUFFER_SIZE / 1000} kb");
      TotalElapsed += PollInterval;
      if(TotalElapsed > 500)
      {
        Console.WriteLine("Calling write buffer!");
        TotalElapsed = 0;
        WriteTimer.Enabled = false;
        WriteBuffer();
      }
    }

    private void WriteBuffer()
    {
      lock (Buffer)
      {
        if (Buffer.Count == 0) return;
        Console.WriteLine($"Writing buffer: {Buffer.Count} / {CURR_BUFFER_SIZE/1000} kb");
        IsWriting = true;
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
        }
        Buffer.Clear();
        TotalElapsed = 0;
        CURR_BUFFER_SIZE = 0;
        IsWriting = false;
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
      CURR_BUFFER_SIZE += System.Text.Encoding.UTF8.GetByteCount(serializedObject);

      lock (Buffer)
      {
        Buffer.Add(hash, serializedObject);
      }

      if (CURR_BUFFER_SIZE > MAX_BUFFER_SIZE)
      {
        WriteBuffer();
      } else
      {
        WriteTimer.Enabled = true;
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
