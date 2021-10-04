using JetBrains.Annotations;

namespace SpeckleConnectionManagerUI.Models
{
    public class SqliteContent
    {
        public string id { get; set; }
        public bool isDefault { get; set; }
        public string token { get; set; }
        public object user { get; set; }
        public ServerInfo serverInfo { get; set; }
        public string refreshToken { get; set; }
    }
}

public class ServerInfo {
    public string name { get; set; }
    public string url {get; set; }
}