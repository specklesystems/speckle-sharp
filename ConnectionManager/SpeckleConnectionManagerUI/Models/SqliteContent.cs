using JetBrains.Annotations;

namespace SpeckleConnectionManagerUI.Models
{
    public partial class Row
    {
        public string hash { get; set; }
        public SqliteContent content { get; set; }
    }

    public partial class SqliteContent
    {
        public string id { get; set; }
        public bool isDefault { get; set; }
        public string token { get; set; }
        public UserInfo userInfo { get; set; }
        public ServerInfo serverInfo { get; set; }
        public string refreshToken { get; set; }
    }

    public partial class ServerInfo
    {
        public string name { get; set; }
        public string url { get; set; }
    }

    public partial class Tokens
    {
        public string token { get; set; }
        public string refreshToken { get; set; }
    }

    public class UserInfo
    {
        public string id { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string company { get; set; }
    }
}

