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
    }
}