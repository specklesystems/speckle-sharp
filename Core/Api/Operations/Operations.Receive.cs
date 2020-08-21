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
    /// Pulls an object from a local transport and deserializes it.
    /// </summary>
    /// <param name="objectId"></param>
    /// <param name="localTransport"></param>
    /// <param name="onProgressAction">An action that is invoked with a dictionary argument containing key value pairs of (process name, processed items).</param>
    /// <returns></returns>
    public static async Task<Base> Receive(string objectId, ITransport localTransport = null, Action<ConcurrentDictionary<string, int>> onProgressAction = null)
    {
      Log.AddBreadcrumb("Receive local");

      var (serializer, settings) = GetSerializerInstance();

      var localProgressDict = new ConcurrentDictionary<string, int>();
      var internalProgressAction = new Action<string, int>((name, processed) =>
      {
        if (localProgressDict.ContainsKey(name))
          localProgressDict[name] += processed;
        else
          localProgressDict[name] = processed;
        onProgressAction?.Invoke(localProgressDict);
      });

      localTransport = localTransport != null ? localTransport : new SqlLiteObjectTransport();
      serializer.Transport = localTransport;
      serializer.OnProgressAction = internalProgressAction;

      var objString = localTransport.GetObject(objectId);

      if (objString == null)
      {
        Log.CaptureException(new SpeckleException($"Object not found in the local cache."), level:SentryLevel.Info);
        throw new SpeckleException($"Object {objectId} was not found in the local cache.");
      }
      else
      {
        return JsonConvert.DeserializeObject<Base>(objString, settings);
      }
    }

    /// <summary>
    /// Pulls an object from a Speckle server. If found in the local transport, that will be used.
    /// </summary>
    /// <param name="objectId"></param>
    /// <param name="streamId"></param>
    /// <param name="client"></param>
    /// <param name="onProgressAction"></param>
    /// <returns></returns>
    public static async Task<Base> Receive(string objectId, string streamId, Client client, ITransport localTransport = null, Action<ConcurrentDictionary<string, int>> onProgressAction = null)
    {
      try
      {
        // try receive from local cache
        return await Receive(objectId, localTransport, onProgressAction);
      }
      catch {  }

      Log.AddBreadcrumb("Receive");

      var (serializer, settings) = GetSerializerInstance();

      var localProgressDict = new ConcurrentDictionary<string, int>();
      var internalProgressAction = new Action<string, int>((name, processed) =>
      {
        if (localProgressDict.ContainsKey(name))
          localProgressDict[name] += processed;
        else
          localProgressDict[name] = processed;
        onProgressAction?.Invoke(localProgressDict);
      });

      localTransport = localTransport != null ? localTransport : new SqlLiteObjectTransport();

      var rem = new RemoteTransport(client.ServerUrl, streamId, client.ApiToken, 1000) { OnProgressAction = internalProgressAction };
        rem.LocalTransport = localTransport;
        var res = await rem.GetObjectAndChildren(objectId);
        await localTransport.WriteComplete(); // wait for the remote transport to write to the local one.
        return JsonConvert.DeserializeObject<Base>(res, settings);
    }

  }
}
