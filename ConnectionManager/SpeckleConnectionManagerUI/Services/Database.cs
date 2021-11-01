using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleConnectionManagerUI.Models;
using SpeckleConnectionManagerUI.ViewModels;

namespace SpeckleConnectionManagerUI.Services
{
    public class Database
    {
        public IEnumerable<ConnectStatusItem> GetItems()
        {
            var savedConnections = Sqlite.GetData();

            var connectStatuses = new List<ConnectStatusItem>();
            int identifier = 0;

            foreach (var savedConnection in savedConnections)
            {
                var connectStatus = new ConnectStatusItem();
                connectStatus.ServerUrl = savedConnection.serverInfo.url;
                connectStatus.ServerName = savedConnection.serverInfo.name;
                connectStatus.Identifier = identifier;
                connectStatus.Disconnected = false;
                connectStatus.Default = savedConnection.isDefault;
                connectStatus.DefaultServerLabel = connectStatus.Default ? "DEFAULT" : "SET AS DEFAULT";
                connectStatus.Colour = connectStatus.Disconnected ? "Red" : "Green";

                if (!connectStatuses.Contains(connectStatus)) connectStatuses.Add(connectStatus);
                identifier++;
            }

            var defaultConnectStatuses = new List<ConnectStatusItem>()
            {
                new ConnectStatusItem
                    {ServerUrl = "https://v2.speckle.arup.com", Identifier = 0},
                //new ConnectStatusItem
                //    {ServerUrl = "https://p500.speckle.arup.com", Identifier = 1}
            };

            var connectedServerUrls = connectStatuses.Select(x => x.ServerUrl).ToList();

            foreach (var defaultConnectStatus in defaultConnectStatuses)
            {
                if (!connectedServerUrls.Contains(defaultConnectStatus.ServerUrl))
                {                   
                    var connected = savedConnections.Any(savedConnection => savedConnection.serverInfo.url == defaultConnectStatus.ServerUrl);

                    var _serverName = savedConnections.Where(x => x.serverInfo.url == defaultConnectStatus.ServerUrl).Select(x => x.serverInfo.name).FirstOrDefault();
                    var _default = savedConnections.Where(x => x.serverInfo.url == defaultConnectStatus.ServerUrl).Select(x => x.isDefault).FirstOrDefault();

                    defaultConnectStatus.ServerName = _serverName;
                    defaultConnectStatus.Disconnected = !connected;
                    defaultConnectStatus.Default = _default;
                    defaultConnectStatus.DefaultServerLabel = _default ? "DEFAULT" : "SET AS DEFAULT";
                    defaultConnectStatus.Colour = connected ? "Green" : "Red";

                    if (!connectStatuses.Contains(defaultConnectStatus)) connectStatuses.Add(defaultConnectStatus);
                }

            }

            return connectStatuses.ToArray();
        }
    }
}