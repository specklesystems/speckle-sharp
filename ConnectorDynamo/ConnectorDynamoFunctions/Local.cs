using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.DesignScript.Runtime;
using Dynamo.Graph.Nodes;
using Speckle.ConnectorDynamo.Functions;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.ConnectorDynamo.Functions
{
  public static class Local
  {
    /// <summary>
    /// Sends data locally, without the need of a Speckle Server
    /// </summary>
    /// <param name="data">Data to send</param>
    /// <returns name="localDataId">ID of the local data sent</returns>
    public static string Send([ArbitraryDimensionArrayImport] object data)
    {
      Tracker.TrackEvent(Tracker.SEND_LOCAL);

      var converter = new BatchConverter();
      var @base = converter.ConvertRecursivelyToSpeckle(data);
      var objectId = Operations.Send(@base).Result;

      return objectId;
    }

    /// <summary>
    /// Receives data locally, without the need of a Speckle Server
    /// NOTE: updates will not be automatically received.
    /// </summary>
    /// <param name="localDataId">ID of the local data to receive</param>
    /// <returns name="data">Data received</returns>
    public static object Receive(string localDataId)
    {
      Tracker.TrackEvent(Tracker.RECEIVE_LOCAL);

      var @base = Operations.Receive(localDataId).Result;
      var converter = new BatchConverter();
      var data = converter.ConvertRecursivelyToNative(@base);
      return data;
    }
  }
}
