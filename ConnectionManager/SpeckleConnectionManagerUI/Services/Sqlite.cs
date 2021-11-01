using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using SpeckleConnectionManagerUI.Models;

namespace SpeckleConnectionManagerUI.Services
{
    public class Sqlite
    {
        public static List<SqliteContent> GetData()
        {
            List<SqliteContent?> entries = new List<SqliteContent>();

            using (SqliteConnection db =
                new SqliteConnection($"Filename={Constants.DatabasePath}"))
            {
                db.Open();

                var createTableCommand = db.CreateCommand();
                createTableCommand.CommandText =
                    @"
                    CREATE TABLE IF NOT EXISTS objects (hash varchar, content varchar);
                ";
                createTableCommand.ExecuteNonQuery();

                SqliteCommand selectCommand = new SqliteCommand
                    ("SELECT content from objects", db);

                SqliteDataReader query = selectCommand.ExecuteReader();

                while (query.Read())
                {
                    var row = query.GetString(0);
                    entries.Add(JsonSerializer.Deserialize<SqliteContent>(row));
                }

                db.Close();
            }

            return entries;
        }

        public static void DeleteAuthData()
        {
            using (SqliteConnection db =
                new SqliteConnection($"Filename={Constants.DatabasePath}"))
            {
                db.Open();
                var truncateTableCommand = db.CreateCommand();
                truncateTableCommand.CommandText =
                    @"
                    DELETE FROM objects;
                ";
                truncateTableCommand.ExecuteNonQuery();

            }
        }

        public static void SetDefaultServer(string serverUrl, bool isDefault)
        {
            using (SqliteConnection db =
                new SqliteConnection($"Filename={Constants.DatabasePath}"))
            {
                db.Open();

                SqliteCommand selectCommand = new SqliteCommand
                    ($"SELECT * from objects WHERE instr(content, '{serverUrl}') > 0", db);

                SqliteDataReader query = selectCommand.ExecuteReader();

                while (query.Read())
                {
                    var objs = new object[3];
                    query.GetValues(objs);
                    var hash = objs[0].ToString();
                    var storedContent = JsonSerializer.Deserialize<SqliteContent>(objs[1].ToString());

                    // If the url is already stored update otherwise create a new entry.
                    if (storedContent != null)
                    {
                        var updateCommand = db.CreateCommand();
                        updateCommand.CommandText =
                            @"
                        UPDATE objects
                        SET content = @content
                        WHERE hash = @hash
                    ";

                        storedContent.isDefault = isDefault;

                        updateCommand.Parameters.AddWithValue("@hash", hash);
                        updateCommand.Parameters.AddWithValue("@content", JsonSerializer.Serialize(storedContent));
                        updateCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        public static void RemoveServer(string serverUrl)
        {
            using (SqliteConnection db =
                new SqliteConnection($"Filename={Constants.DatabasePath}"))
            {
                db.Open();

                SqliteCommand deleteCommand = new SqliteCommand($"DELETE from objects WHERE instr(content, '{serverUrl}') > 0", db);

                deleteCommand.ExecuteNonQuery();
            }
        }
    }
}