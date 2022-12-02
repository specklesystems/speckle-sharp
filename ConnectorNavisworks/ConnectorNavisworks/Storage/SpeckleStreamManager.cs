using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Data;
using DesktopUI2.Models;
using System;
using System.Collections.Generic;
using Autodesk.Navisworks.Api.DocumentParts;
using Speckle.Newtonsoft.Json;
using System.Data;
using static Speckle.ConnectorNavisworks.Utils;

namespace Speckle.ConnectorNavisworks.Storage
{
    internal class SpeckleStreamManager
    {
        internal static readonly string TableName = "speckle";
        internal static readonly string KeyName = "stream_states";

        public static List<StreamState> ReadState(Document doc)
        {
            var streams = new List<StreamState>();
            if (doc == null) return streams;

            DocumentDatabase database = doc.Database;
            NavisworksDataAdapter dataAdapter =
                new NavisworksDataAdapter($"SELECT value FROM {TableName} WHERE key = '{KeyName}'", database.Value);

            DataTable table = new DataTable();
            try
            {
                dataAdapter.Fill(table);
            }
            catch (DatabaseException)
            {
                WarnLog($"We didn't find the speckle data store. That's ok - we'll make one later");
            }

            if (table.Rows.Count <= 0)
            {
                ConsoleLog("No saved streams found.");
                return streams;
            }

            DataRow row = table.Rows[0];

            var speckleStreamsStore = row["value"];

            if (speckleStreamsStore == null) return streams;
            try
            {
                streams = JsonConvert.DeserializeObject<List<StreamState>>((string)speckleStreamsStore);

                if (streams == null || streams.Count <= 0)
                {
                    ErrorLog(
                        $"Something isn't right. " +
                        $"{KeyName} was found but didn't deserialize into any streams:" +
                        $"\n {speckleStreamsStore}");
                }
                else
                {
                    ConsoleLog($"{streams.Count} saved streams found.");
                }
            }
            catch (Exception ex)
            {
                ErrorLog($"Deserialization failed: {ex.Message}");
            }

            return streams;
        }

        internal static void WriteStreamStateList(Document doc, List<StreamState> streamStates)
        {
            if (doc == null) return;

            DocumentDatabase documentDatabase = doc.Database;

            using (NavisworksTransaction transaction = documentDatabase.BeginTransaction(DatabaseChangedAction.Reset))
            {
                NavisworksCommand command = transaction.Connection.CreateCommand();
                string sql = $"CREATE TABLE IF NOT EXISTS {TableName}(key TEXT, value TEXT)";
                command.CommandText = sql;
                var affected = command.ExecuteNonQuery();
                transaction.Commit();
            }

            var list = ReadState(doc);

            using (NavisworksTransaction transaction = documentDatabase.BeginTransaction(DatabaseChangedAction.Edited))
            {
                NavisworksCommand command = transaction.Connection.CreateCommand();
                var streamStatesStore = JsonConvert.SerializeObject(streamStates) as string;
                command.Parameters.AddWithValue("@p1", $"{KeyName}");
                command.Parameters.AddWithValue("@p2", streamStatesStore);

                var sql = list.Count > 0
                    ? $"UPDATE {TableName} SET key = @p1, value = @p2 WHERE key = '{KeyName}';"
                    : $"INSERT INTO {TableName}(key, value) VALUES(@p1, @p2);";

                try
                {
                    command.CommandText = sql;
                    var affected = command.ExecuteNonQuery();
                    if (affected>0) ConsoleLog($"{streamStates.Count} saved streams found.");
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    ErrorLog($"Something went wrong: {ex.Message}");
                }
            }
        }
    }
}