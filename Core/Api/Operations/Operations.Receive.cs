using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using System.Collections.Concurrent;
using Speckle.Core.Logging;
using Sentry.Protocol;

namespace Speckle.Core.Api
{
  public static partial class Operations
  {

    /// <summary>
    /// Receives an object from a transport.
    /// </summary>
    /// <param name="objectId"></param>
    /// <param name="remoteTransport">The transport to receive from.</param>
    /// <param name="localTransport">Leave null to use the default cache.</param>
    /// <param name="onProgressAction"></param>
    /// <returns></returns>
    public static async Task<Base> Receive(string objectId, ITransport remoteTransport = null, ITransport localTransport = null, Action<ConcurrentDictionary<string, int>> onProgressAction = null)
    {
      Log.AddBreadcrumb("Receive");

      var (serializer, settings) = GetSerializerInstance();

      var localProgressDict = new ConcurrentDictionary<string, int>();
      var internalProgressAction = GetInternalProgressAction(localProgressDict, onProgressAction);

      localTransport = localTransport != null ? localTransport : new SQLiteTransport();

      serializer.ReadTransport = localTransport;
      serializer.OnProgressAction = internalProgressAction;

      var objString = localTransport.GetObject(objectId);

      if (objString != null)
      {
        return JsonConvert.DeserializeObject<Base>(objString, settings);
      } else if( remoteTransport == null )
      {
        Log.CaptureAndThrow(new SpeckleException($"Could not find specified object using the local transport, and you didn't provide a fallback remote from which to pull it."), SentryLevel.Error);
      }

      Log.AddBreadcrumb("RemoteHit");
      objString = await remoteTransport.CopyObjectAndChildren(objectId, localTransport);

      await localTransport.WriteComplete();

      return JsonConvert.DeserializeObject<Base>(objString, settings);
    }

  }
}
