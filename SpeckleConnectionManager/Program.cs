using System.Diagnostics;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.IO;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using System.Text.Json.Serialization;

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


            if(!Directory.Exists(appFolderFullName)) {
                Directory.CreateDirectory(appFolderFullName);
            }

            string serverLocationStore = Path.Combine(appFolderFullName, "server.txt");
            string challengeCodeStore = Path.Combine(appFolderFullName, "challenge.txt");


            if (args.Length == 0) {
                Console.Write("Speckle Server Address (e.g. https://v2.speckle.arup.com): ");
                var url = Console.ReadLine();
                var code = Guid.NewGuid();
                var suuid= Guid.NewGuid();
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = $"{url}/authn/verify/sdm/{code}?suuid={suuid}",
                    UseShellExecute = true
                };
                Process.Start(psi);
                // Process.Start("open", $"{url}/authn/verify/sdm/{code}?suuid={suuid}"); //MAC

                System.IO.File.WriteAllText(serverLocationStore, url);
                System.IO.File.WriteAllText(challengeCodeStore, code.ToString());
            } else {
                var accessCode = args[0].Split('=')[1];
                var savedUrl = System.IO.File.ReadAllText(serverLocationStore);
                var savedChallenge = System.IO.File.ReadAllText(challengeCodeStore);

                HttpResponseMessage response = await client.PostAsJsonAsync($"{savedUrl}/auth/token", new {
                    appId = "sdm",
                    appSecret = "sdm",
                    accessCode = accessCode,
                    challenge = savedChallenge
                });
                var tokens = await response.Content.ReadFromJsonAsync<Tokens>();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokens.token}");

                var info = await client.PostAsJsonAsync($"{savedUrl}/graphql", new {
                    query = "{\n  user {\n    id\n    email\n    name\n company \n} serverInfo {\n name \n company \n canonicalUrl \n }\n}\n"
                });

                var infoContent = await info.Content.ReadFromJsonAsync<InfoData>();

                var serverInfo = infoContent.data.serverInfo;
                serverInfo.url = savedUrl;

                var content = new {
                    id = infoContent.data.serverInfo.name,
                    isDefault = true,
                    token = tokens.token,
                    userInfo = infoContent.data.user,
                    serverInfo = infoContent.data.serverInfo,
                    refreshToken = tokens.refreshToken
                };

                string jsonString = JsonSerializer.Serialize(content);

                string dbPath = Path.Combine(appFolderFullName, "Accounts.db");
                var connection = new SqliteConnection($"Data Source={dbPath}");
                connection.Open();
                var command = connection.CreateCommand();

                var createTableCommand = connection.CreateCommand();
                createTableCommand.CommandText =
                @"
                    CREATE TABLE IF NOT EXISTS objects (hash varchar, content varchar);
                ";
                createTableCommand.ExecuteNonQuery();

                command.CommandText = 
                @"
                    INSERT INTO objects (hash, content)
                    VALUES (@hash, @content);
                ";
                command.Parameters.AddWithValue("@hash", content.GetHashCode());
                command.Parameters.AddWithValue("@content", jsonString);
                command.ExecuteNonQuery();
            }

                


  

            return "";
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

    public class Info {
        public object user  { get; set; }
        public ServerInfo serverInfo { get; set; }
    }

    public class ServerInfo {
        public String name { get; set; }
        public String url {get; set; }
    }
}
