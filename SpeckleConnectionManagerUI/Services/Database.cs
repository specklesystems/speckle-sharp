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

            var connectStatuses = new[]
            {
                new ConnectStatusItem
                    {ServerName = "v2.speckle.arup.com", Identifier = 0},
                new ConnectStatusItem
                    {ServerName = "p500.speckle.arup.com", Identifier = 1}
            };

            var savedConnections = Sqlite.GetData();

            foreach (var connectStatus in connectStatuses)
            {
                var connected = savedConnections.Any(savedConnection =>
                    savedConnection.serverInfo.url == $"https://{connectStatus.ServerName}");

                connectStatus.Disconnected = !connected;
                connectStatus.Colour = connected ? "Green" : "Red";
            }

            
            return connectStatuses;
        }
    }
}