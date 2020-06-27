using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Speckle.Models;
using Speckle.Serialisation;
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
    public static async Task<string> Push(Base @object, SqlLiteObjectTransport localTransport = null, IEnumerable<Remote> remotes = null, Action<string, int> onProgressAction = null)
    {
      var (serializer, settings) = GetSerializerInstance();

      serializer.Transport = localTransport != null ? localTransport : new SqlLiteObjectTransport();
      serializer.OnProgressAction = onProgressAction;

      if (remotes != null)
        foreach (var remote in remotes)
        {
          serializer.SecondaryWriteTransports.Add(new RemoteTransport(remote.ServerUrl, remote.StreamId, remote.ApiToken) { LocalTransport = serializer.Transport });
        }

      var obj = JsonConvert.SerializeObject(@object, settings);
      var hash = JObject.Parse(obj).GetValue("id").ToString();

      await Transports.Utilities.WaitUntil(() =>
      {
        foreach (var t in serializer.SecondaryWriteTransports)
        {
          if (!((RemoteTransport)t).GetWriteCompletionStatus()) return false;
        }
        if (!localTransport.GetWriteCompletionStatus()) return false;
        return true;
      }, 500);

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
    public static async Task<List<string>> Push(IEnumerable<Base> objects, SqlLiteObjectTransport localTransport = null, IEnumerable<Remote> remotes = null, Action<string, int> onProgressAction = null)
    {
      var (serializer, settings) = GetSerializerInstance();

      serializer.Transport = localTransport != null ? localTransport : new SqlLiteObjectTransport();
      serializer.OnProgressAction = onProgressAction;

      if (remotes != null)
        foreach (var remote in remotes)
        {
          serializer.SecondaryWriteTransports.Add(new RemoteTransport(remote.ServerUrl, remote.StreamId, remote.ApiToken) { LocalTransport = serializer.Transport });
        }

      var obj = JsonConvert.SerializeObject(objects, settings);
      var res = JsonConvert.DeserializeObject<List<ObjectReference>>(obj);

      await Transports.Utilities.WaitUntil(() =>
      {
        foreach (var t in serializer.SecondaryWriteTransports)
        {
          if (!((RemoteTransport)t).GetWriteCompletionStatus()) return false;
        }
        if (!localTransport.GetWriteCompletionStatus()) return false;
        return true;
      }, 500);

      return res.Select(o => o.referencedId).ToList();
    }

    /// <summary>
    /// Pushes a previously serialized object (and its children) to the given remotes.
    /// </summary>
    /// <param name="objectId"></param>
    /// <param name="localTransport"></param>
    /// <param name="remotes"></param>
    /// <param name="onProgressAction"></param>
    /// <returns></returns>
    public static async Task<string> Push(string objectId, SqlLiteObjectTransport localTransport = null, IEnumerable<Remote> remotes = null, Action<string, int> onProgressAction = null)
    {
      var remoteTransports = new List<RemoteTransport>();

      localTransport = localTransport == null ? new SqlLiteObjectTransport() : localTransport;

      foreach (var remote in remotes)
      {
        remoteTransports.Add(new RemoteTransport(remote.ServerUrl, remote.StreamId, remote.ApiToken) { LocalTransport = localTransport });
      }

      var obj = localTransport.GetObject(objectId);
      var childrenIds = JObject.Parse(obj).GetValue("__closure").ToObject<Dictionary<string, int>>().Keys.ToArray();

      foreach (var t in remoteTransports)
      {
        t.SaveObject(objectId, obj);
        foreach (var childId in childrenIds)
        {
          var childObj = localTransport.GetObject(childId);
          t.SaveObject(childId, childObj);
        }
      }

      await Transports.Utilities.WaitUntil(() =>
      {
        foreach (var t in remoteTransports)
        {
          if (!t.GetWriteCompletionStatus()) return false;
        }
        return true;
      }, 500);

      return objectId;
    }

    #endregion

  }
}