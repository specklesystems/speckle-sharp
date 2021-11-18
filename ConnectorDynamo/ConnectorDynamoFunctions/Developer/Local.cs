using System.Threading.Tasks;
using Autodesk.DesignScript.Runtime;
using Speckle.Core.Api;
using Speckle.Core.Logging;

namespace Speckle.ConnectorDynamo.Functions.Developer
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
      Tracker.TrackPageview(Tracker.SEND_LOCAL);

      var converter = new BatchConverter();
      var @base = converter.ConvertRecursivelyToSpeckle(data);
      var objectId = Task.Run(async () => await Operations.Send(@base, disposeTransports: true)).Result;

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
      Tracker.TrackPageview(Tracker.RECEIVE_LOCAL);

      var @base = Task.Run(async () => await Operations.Receive(localDataId, disposeTransports: true)).Result;
      var converter = new BatchConverter();
      var data = converter.ConvertRecursivelyToNative(@base);
      return data;
    }
  }
}
