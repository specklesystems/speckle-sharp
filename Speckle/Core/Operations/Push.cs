using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Speckle.Models;
using Speckle.Transports;

namespace Speckle.Core
{
  public static partial class Operations
  {
    #region Pushing objects
    /// <summary>
    /// Pushes and transports an object, as well as any of its detachable children, to all the transports provided.
    /// <para>This method is an integrated serialization and transportation step. If no remotes are provided, it will only push to a local cache.</para>
    /// </summary>
    /// <param name="object"></param>
    /// <param name="localTransport"></param>
    /// <param name="remotes"></param>
    /// <param name="onProgressAction"></param>
    /// <returns>The object's id (hash).</returns>
    public static async Task<string> Push(Base @object, ITransport localTransport = null, IEnumerable<Remote> remotes = null, Action<ConcurrentDictionary<string, int>> onProgressAction = null)
    {
      var (serializer, settings) = GetSerializerInstance();
      localTransport = localTransport != null ? localTransport : new SqlLiteObjectTransport();

      var localProgressDict = new ConcurrentDictionary<string, int>();
      var internalProgressAction = new Action<string, int>((name, processed) =>
      {
        if (localProgressDict.ContainsKey(name))
          localProgressDict[name] += processed;
        else
          localProgressDict[name] = processed;
        onProgressAction?.Invoke(localProgressDict);
      });

      serializer.Transport = localTransport;
      serializer.OnProgressAction = internalProgressAction;

      if (remotes != null)
      {
        foreach (var remote in remotes)
        {
          serializer.SecondaryWriteTransports.Add(new RemoteTransport(remote.ServerUrl, remote.StreamId, remote.ApiToken)
          {
            LocalTransport = serializer.Transport,
            OnProgressAction = internalProgressAction
          });
        }
      }

      var obj = JsonConvert.SerializeObject(@object, settings);
      var hash = JObject.Parse(obj).GetValue("id").ToString();

      var transportAwaits = serializer.SecondaryWriteTransports.Select(t => t.WriteComplete()).ToList();
      transportAwaits.Add(localTransport.WriteComplete());

      await Task.WhenAll(transportAwaits);

      return hash;
    }

    /// <summary>
    /// Serializes and transports a list of objects by first wrapping them into a commit.
    /// </summary>
    /// <param name="objects"></param>
    /// <param name="localTransport"></param>
    /// <param name="remotes"></param>
    /// <param name="onProgressAction"></param>
    /// <returns>The commit's id (hash).</returns>
    public static async Task<List<string>> Push(IEnumerable<Base> objects, SqlLiteObjectTransport localTransport = null, IEnumerable<Remote> remotes = null, Action<string, int> onProgressAction = null, Action<string, int> onRemoteProgressAction = null)
    {
      var (serializer, settings) = GetSerializerInstance();
      localTransport = localTransport != null ? localTransport : new SqlLiteObjectTransport();

      serializer.Transport = localTransport;
      serializer.OnProgressAction = onProgressAction;

      if (remotes != null)
      {
        foreach (var remote in remotes)
        {
          serializer.SecondaryWriteTransports.Add(new RemoteTransport(remote.ServerUrl, remote.StreamId, remote.ApiToken) { LocalTransport = serializer.Transport, OnProgressAction = onRemoteProgressAction });
        }
      }

      var obj = JsonConvert.SerializeObject(objects, settings);
      var res = JsonConvert.DeserializeObject<List<ObjectReference>>(obj);

      var transportAwaits = serializer.SecondaryWriteTransports.Select(t => t.WriteComplete()).ToList();
      transportAwaits.Add(localTransport.WriteComplete());

      await Task.WhenAll(transportAwaits);

      return res.Select(o => o.referencedId).ToList();
    }

    #endregion

  }
}