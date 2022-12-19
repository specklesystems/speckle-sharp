using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Dynamo.Graph.Nodes;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;
using Speckle.Core.Transports;

namespace Speckle.ConnectorDynamo.Functions.Developer
{
  public static class Transport
  {
    /// <summary>
    /// Creates a Disk Transport.
    /// </summary>
    /// <param name="basePath">The root folder where you want the data to be stored. Defaults to `%appdata%/Speckle/DiskTransportFiles`.</param>
    /// <returns name="transport">The Disk Transport you have created.</returns>
    [NodeCategory("Transports")]
    public static object DiskTransport(string basePath = "")
    {
      if (string.IsNullOrEmpty(basePath))
        basePath = Path.Combine(SpecklePathProvider.UserSpeckleFolderPath, "DiskTransportFiles");

      Analytics.TrackEvent(Analytics.Events.NodeRun, new Dictionary<string, object>() { { "name", "Disk Transport" } });

      return new DiskTransport.DiskTransport(basePath);
    }

    /// <summary>
    /// Creates an Memory Transport.
    /// </summary>
    /// <param name="name">The name of this Memory Transport.</param>
    /// <returns name="transport">The Memory Transport you have created.</returns>
    [NodeCategory("Transports")]
    public static object MemoryTransport(string name = "Memory")
    {
      Analytics.TrackEvent(Analytics.Events.NodeRun, new Dictionary<string, object>() { { "name", "Memory Transport" } });
      return new MemoryTransport { TransportName = name };
    }

    /// <summary>
    /// Creates a Server Transport.
    /// </summary>
    /// <param name="stream">The Stream you want to send data to.</param>
    /// <returns name="transport">The Server Transport you have created.</returns>
    [NodeCategory("Transports")]
    public static object ServerTransport(StreamWrapper stream)
    {

      var userId = stream.UserId;
      Core.Credentials.Account account;

      account = AccountManager.GetAccounts().FirstOrDefault(a => a.userInfo.id == userId);
      Exception error = null;
      if (account == null)
      {
        // Get the default account
        account = AccountManager.GetAccounts(stream.ServerUrl).FirstOrDefault();
        error = new WarningException(
          "Original account not found. Please make sure you have permissions to access this stream!");
        if (account == null)
        {
          // No default
          error = new WarningException(
            $"No account found for {stream.ServerUrl}.");
        }
      }

      if (error != null) throw error;

      Analytics.TrackEvent(Analytics.Events.NodeRun, new Dictionary<string, object>() { { "name", "Server Transport" } });


      return new ServerTransport(account, stream.StreamId);
    }

    /// <summary>
    /// Creates an SQLite Transport.
    /// </summary>
    /// <param name="basePath">The root folder where you want the sqlite db to be stored. Defaults to `%appdata%`.</param>
    /// <param name="applicationName">The subfolder you want the sqlite db to be stored. Defaults to `Speckle`.</param>
    /// <param name="scope">The name of the actual database file. Defaults to `UserLocalDefaultDb`.</param>
    /// <returns></returns>
    [NodeCategory("Transports")]
    public static object SQLiteTransport(string basePath = "", string applicationName = "Speckle", string scope = "UserLocalDefaultDb")
    {
      if (string.IsNullOrEmpty(basePath))
        basePath = null;
      if (string.IsNullOrEmpty(applicationName))
        applicationName = "Speckle";
      if (string.IsNullOrEmpty(scope))
        scope = "UserLocalDefaultDb";

      Analytics.TrackEvent(Analytics.Events.NodeRun, new Dictionary<string, object>() { { "name", "SQLite Transport" } });

      return new SQLiteTransport(basePath, applicationName, scope);
    }
  }
}
