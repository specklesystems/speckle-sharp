using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Speckle.Models;
using Speckle.Transports;
using System.Collections.Concurrent;

namespace Speckle.Core
{
  public static partial class Operations
  {

    /// <summary>
    /// Pulls an object and deserializes it. If found in the local transport, the remote will not be used.
    /// </summary>
    /// <param name="objectId"></param>
    /// <param name="localTransport"></param>
    /// <param name="remote"></param>
    /// <param name="onProgressAction">An action that is invoked with a dictionary argument containing key value pairs of (process name, processed items).</param>
    /// <returns></returns>
    public static async Task<Base> Receive(string objectId, ITransport localTransport = null, Remote remote = null, Action<ConcurrentDictionary<string, int>> onProgressAction = null)
    {
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

      if (objString == null && remote == null)
      {
        throw new Exception($"Object {objectId} was not found in the local cache. Please provide a remote from which to pull it.");
      }
      else if (objString == null)
      {
        var rem = new RemoteTransport(remote.ServerUrl, remote.StreamId, remote.ApiToken, 1000) { OnProgressAction = internalProgressAction };
        rem.LocalTransport = localTransport;
        var res = await rem.GetObjectAndChildren(objectId);
        await localTransport.WriteComplete(); // wait for the remote transport to write to the local one.
        return JsonConvert.DeserializeObject<Base>(res, settings);
      }
      else
      {
        return JsonConvert.DeserializeObject<Base>(objString, settings);
      }
    }

  }
}