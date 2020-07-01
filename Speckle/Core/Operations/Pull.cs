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

    public static async Task<Base> Pull(string objectId, ITransport localTransport = null, Remote remote = null, Action<ConcurrentDictionary<string, int>> onProgressAction = null)
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
        var rem = new RemoteTransport("http://localhost:3000", "lol", "lol", 1000) { OnProgressAction = internalProgressAction };
        rem.LocalTransport = localTransport;
        var res = await rem.GetObjectAndChildren(objectId);
        await localTransport.WriteComplete();
        return JsonConvert.DeserializeObject<Base>(res, settings);
      }
      else
      {
        return JsonConvert.DeserializeObject<Base>(objString, settings);
      }
    }

  }
}