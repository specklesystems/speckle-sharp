using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.IO;
using System.Net;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace SpeckleConnectionManager
{
    class Program
    {
        static HttpClient client = new HttpClient();
        static void Main(string[] args)
        {
            Run(args).GetAwaiter().GetResult();
        }

        static async Task<string> Run(string[] args)
        {
            string appDataFolder =
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            string appFolderFullName = Path.Combine(appDataFolder, "Speckle");


            if (!Directory.Exists(appFolderFullName))
            {
                Directory.CreateDirectory(appFolderFullName);
            }

            var serverLocationStore = Path.Combine(appFolderFullName, "server.txt");
            var challengeCodeStore = Path.Combine(appFolderFullName, "challenge.txt");

            if (!args[0].Contains("access_code"))
            {
                RestartConnectionManagerUI(appDataFolder);
                return "";
            }

            var accessCode = args[0].Split('=')[1];
            var savedUrl = File.ReadAllText(serverLocationStore);
            var savedChallenge = File.ReadAllText(challengeCodeStore);

            var response = await client.PostAsJsonAsync($"{savedUrl}/auth/token", new
            {
                appId = "sdm",
                appSecret = "sdm",
                accessCode,
                challenge = savedChallenge
            });

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine($"Failed to auth: {response.StatusCode}");
                return "";
            };

            var tokens = await response.Content.ReadFromJsonAsync<Tokens>();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokens.token}");

            var info = await client.PostAsJsonAsync($"{savedUrl}/graphql", new
            {
                query = "{\n  user {\n    id\n    email\n    name\n company \n} serverInfo {\n name \n company \n canonicalUrl \n }\n}\n"
            });

            if (info.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine($"Failed to auth: {info.StatusCode}");
                return "";
            }

            var infoContent = await info.Content.ReadFromJsonAsync<InfoData>();

            if (infoContent == null) return "";

            var serverInfo = infoContent.data.serverInfo;
            serverInfo.url = savedUrl;

            var content = new
            {
                id = infoContent.data.serverInfo.name,
                isDefault = false,
                tokens.token,
                userInfo = infoContent.data.user,
                infoContent.data.serverInfo,
                tokens.refreshToken
            };

            string jsonString = JsonSerializer.Serialize(content);

            string dbPath = Path.Combine(appFolderFullName, "Accounts.db");
            var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText =
            @"
                CREATE TABLE IF NOT EXISTS objects (hash varchar, content varchar);
            ";
            createTableCommand.ExecuteNonQuery();

            SqliteCommand selectCommand = new SqliteCommand
                ("SELECT * from objects", connection);

            SqliteDataReader query = selectCommand.ExecuteReader();

            var entryFound = false;

            while (query.Read())
            {
                var objs = new object[3];
                query.GetValues(objs);
                var hash = objs[0].ToString();
                var storedContent = JsonSerializer.Deserialize<SqliteContent>(objs[1].ToString());

                // If the url is already stored update otherwise create a new entry.
                if (storedContent != null && storedContent.serverInfo.url == content.serverInfo.url)
                {
                    var updateCommand = connection.CreateCommand();
                    updateCommand.CommandText =
                        @"
                        UPDATE objects
                        SET content = @content
                        WHERE hash = @hash
                    ";

                    Console.WriteLine(connection.State);

                    updateCommand.Parameters.AddWithValue("@hash", hash);
                    updateCommand.Parameters.AddWithValue("@content", JsonSerializer.Serialize(content));
                    updateCommand.ExecuteNonQuery();
                    entryFound = true;
                }
            }

            if (!entryFound)
            {
                var command = connection.CreateCommand();

                command.CommandText =
                    @"
                            INSERT INTO objects (hash, content)
                            VALUES (@hash, @content);
                        ";
                command.Parameters.AddWithValue("@hash", content.GetHashCode());
                command.Parameters.AddWithValue("@content", jsonString);
                command.ExecuteNonQuery();
            }

            // Restart the Speckle Connection Manager
            RestartConnectionManagerUI(appDataFolder);

            return "";
        }

        private static void RestartConnectionManagerUI(string appDataFolder)
        {
            Process p = new Process();
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = "taskkill";
            p.StartInfo.Arguments = "/F /IM SpeckleConnectionManagerUI.exe";
            p.Start();
            p.WaitForExit();

            var uiProcess = new Process();
            uiProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            var uiExeFolder = Path.Combine(appDataFolder, "speckle-connection-manager-ui");
            uiProcess.StartInfo.FileName = Path.Combine(uiExeFolder, "SpeckleConnectionManagerUI.exe");
            uiProcess.Start();
        }

    }


    public class Tokens
    {
        public string token { get; set; }
        public string refreshToken { get; set; }
    }

    public class InfoData
    {
        public Info data { get; set; }
    }

    public class Info
    {
        public object user { get; set; }
        public ServerInfo serverInfo { get; set; }
    }

    public class ServerInfo
    {
        public String name { get; set; }
        public String url { get; set; }
    }

    public class Row
    {
        public string hash { get; set; }
        public SqliteContent content { get; set; }
    }

    public class SqliteContent
    {
        public string id { get; set; }
        public bool isDefault { get; set; } = false;
        public string token { get; set; }
        public object user { get; set; }
        public ServerInfo serverInfo { get; set; }
        public string refreshToken { get; set; }
    }
}
