using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Data.Sqlite;
using SpeckleConnectionManagerUI.Models;

namespace SpeckleConnectionManagerUI.Services
{
    class RefreshTokenAction
    {
        public async Task<string> Run()
        {
            HttpClient client = new();
            string appDataFolder =
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            string appFolderFullName = Path.Combine(appDataFolder, "Speckle");


            if (!Directory.Exists(appFolderFullName))
            {
                Directory.CreateDirectory(appFolderFullName);
            }

            string dbPath = Path.Combine(appFolderFullName, "Accounts.db");
            var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            SqliteCommand selectCommand = new SqliteCommand
                    ("SELECT * from objects", connection);

            SqliteDataReader query = selectCommand.ExecuteReader();

            var entries = new List<Row>();

            while (query.Read())
            {
                object[] objs = new object[3];
                var row = query.GetValues(objs);

                entries.Add(new Row
                {
                    hash = objs[0].ToString(),
                    content = JsonSerializer.Deserialize<SqliteContent>(objs[1].ToString())
                });
            }


            foreach (var entry in entries)
            {
                var content = entry.content;
                var url = content.serverInfo.url;
                Console.WriteLine($"Auth token: {content.token}");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {content.token}");
                HttpResponseMessage response = await client.PostAsJsonAsync($"{url}/auth/token", new
                {
                    appId = "sdm",
                    appSecret = "sdm",
                    refreshToken = content.refreshToken,
                });
                client.DefaultRequestHeaders.Remove("Authorization");
                Console.WriteLine(response.StatusCode);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    continue;
                }
                var tokens = await response.Content.ReadFromJsonAsync<Tokens>();
                content.token = tokens.token;
                content.refreshToken = tokens.refreshToken;

                Console.WriteLine(tokens.token);

                var command = connection.CreateCommand();
                command.CommandText =
                    @"
                    UPDATE objects
                    SET content = @content
                    WHERE hash = @hash
                ";

                Console.WriteLine(connection.State);

                command.Parameters.AddWithValue("@hash", content.GetHashCode());
                command.Parameters.AddWithValue("@content", JsonSerializer.Serialize(content));
                command.ExecuteNonQuery();
                Console.WriteLine($"Updated {entry.hash}");
            };

            return "";
        }
    }
}

