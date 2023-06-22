using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Data;
using DesktopUI2.Models;
using Speckle.Newtonsoft.Json;
using static Speckle.ConnectorNavisworks.Other.Utilities;

namespace Speckle.ConnectorNavisworks.Storage;

internal abstract class SpeckleStreamManager
{
  private const string TableName = "speckle";
  private const string KeyName = "stream_states";

  public static List<StreamState> ReadState(Document doc)
  {
    var streams = new List<StreamState>();
    if (doc == null)
      return streams;
    if (doc.Database == null)
      return streams;
    if (doc.ActiveSheet == null)
      return streams;

    var database = doc.Database;

    using var table = new DataTable();
    using (
      var dataAdapter = new NavisworksDataAdapter(
        $"SELECT value FROM {TableName} WHERE key = '{KeyName}'",
        database.Value
      )
    )
      try
      {
        dataAdapter.Fill(table);
      }
      catch (DatabaseException)
      {
        WarnLog("We didn't find the speckle data store. That's ok - we'll make one later");
      }

    if (table.Rows.Count <= 0)
    {
      ConsoleLog("No saved streams found.");
      return streams;
    }

    var row = table.Rows[0];

    if (table.Rows.Count > 1)
    {
      ConsoleLog($"Rebuilding Saved State DB. {table.Rows.Count} is too many.");

      string deleteSql = $"DELETE FROM {TableName} WHERE key = @key";
      string insertSql = $"INSERT INTO {TableName}(key, value) VALUES(@key, @value)";

      using NavisworksTransaction transaction = database.BeginTransaction(DatabaseChangedAction.Edited);
      NavisworksCommand command = transaction.Connection.CreateCommand();

      try
      {
        command.CommandText = deleteSql;
        command.Parameters.AddWithValue("@key", KeyName);

        int unused = command.ExecuteNonQuery();

        command.CommandText = insertSql;
        command.Parameters.AddWithValue("@key", KeyName);
        command.Parameters.AddWithValue("@value", row.ItemArray);

        int inserted = command.ExecuteNonQuery();
        if (inserted > 0)
          ConsoleLog("Stream state stored.");

        transaction.Commit();
      }
      catch (SqlException)
      {
        // ignore
      }

      return streams;
    }

    var speckleStreamsStore = row["value"];

    if (speckleStreamsStore == null)
      return streams;
    try
    {
      streams = JsonConvert.DeserializeObject<List<StreamState>>((string)speckleStreamsStore);

      if (streams == null || streams.Count <= 0)
        ErrorLog(
          "Something isn't right. "
            + $"{KeyName} was found but didn't deserialize into any streams:"
            + $"\n {speckleStreamsStore}"
        );
      else
        ConsoleLog($"{streams.Count} saved streams found in file.");
    }
    catch (JsonException ex)
    {
      ErrorLog($"Deserialization failed: {ex.Message}");
    }

    return streams;
  }

  internal static void WriteStreamStateList(Document doc, List<StreamState> streamStates)
  {
    var documentDatabase = doc?.Database;

    if (documentDatabase == null)
      return;
    if (doc.ActiveSheet == null)
      return;

    string streamStatesStore = JsonConvert.SerializeObject(streamStates);

    string createSql = $"CREATE TABLE IF NOT EXISTS {TableName}(key TEXT, value TEXT)";
    string deleteSql = $"DELETE FROM {TableName} WHERE key = @key";
    string insertSql = $"INSERT INTO {TableName}(key, value) VALUES(@key, @value)";

    using (NavisworksTransaction transaction = documentDatabase.BeginTransaction(DatabaseChangedAction.Reset))
    {
      NavisworksCommand command = transaction.Connection.CreateCommand();
      command.CommandText = createSql;
      var unused = command.ExecuteNonQuery();
      transaction.Commit();
    }

    // TODO! UPDATE would theoretically be faster but the table creation logic would need
    // to create a row if none exist which would be two more queries
    using (NavisworksTransaction transaction = documentDatabase.BeginTransaction(DatabaseChangedAction.Edited))
    {
      NavisworksCommand command = transaction.Connection.CreateCommand();

      try
      {
        command.CommandText = deleteSql;
        command.Parameters.AddWithValue("@key", KeyName);
        int unused = command.ExecuteNonQuery();

        command.CommandText = insertSql;
        command.Parameters.AddWithValue("@key", KeyName);
        command.Parameters.AddWithValue("@value", streamStatesStore);
        int inserted = command.ExecuteNonQuery();

        if (inserted > 0)
          ConsoleLog($"{streamStates.Count} stream states stored.");
        transaction.Commit();
      }
      catch (SqlException ex)
      {
        ErrorLog($"Something went wrong: {ex.Message}");
      }
    }
  }
}
