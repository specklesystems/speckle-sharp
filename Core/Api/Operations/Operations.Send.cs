using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry.Protocol;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.Core.Api
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
    /// <param name="onProgressAction">An action that is invoked with a dictionary argument containing key value pairs of (process name, processed items).</param>
    /// <returns>The object's id (hash).</returns>
    public static async Task<string> Send(Base @object, ITransport localTransport = null, Action<ConcurrentDictionary<string, int>> onProgressAction = null)
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


      var obj = JsonConvert.SerializeObject(@object, settings);
      var hash = JObject.Parse(obj).GetValue("id").ToString();

      var transportAwaits = serializer.SecondaryWriteTransports.Select(t => t.WriteComplete()).ToList();
      transportAwaits.Add(localTransport.WriteComplete());

      await Task.WhenAll(transportAwaits);


      return hash;
    }

    /// <summary>
    /// Pushes and transports an object, as well as any of its detachable children, to all the transports provided.
    /// <para>This method is an integrated serialization and transportation step.</para>
    /// </summary>
    /// <param name="object">Base object to send</param>
    /// <param name="streamIds">List of StreamIds to send the objects to</param>
    /// <param name="clients">List of Clients to use</param>
    /// <param name="localTransport"></param>
    /// <param name="onProgressAction">An action that is invoked with a dictionary argument containing key value pairs of (process name, processed items).</param>
    /// <returns>The object's id (hash).</returns>
    public static async Task<string> Send(Base @object, IEnumerable<string> streamIds, IEnumerable<Client> clients, ITransport localTransport = null, Action<ConcurrentDictionary<string, int>> onProgressAction = null)
    {

      if (streamIds.Count() != clients.Count())
      {
        Log.CaptureAndThrow(new SpeckleException($"The number of streams and clients provided does not match."), SentryLevel.Error);
      }

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


      for (var i = 0; i < clients.Count(); i++)
      {
        var client = clients.ElementAt(i);
        var streamId = streamIds.ElementAt(i);
        serializer.SecondaryWriteTransports.Add(new RemoteTransport(client.ServerUrl, streamId, client.ApiToken)
        {
          LocalTransport = serializer.Transport,
          OnProgressAction = internalProgressAction
        });
      }

      var obj = JsonConvert.SerializeObject(@object, settings);
      var hash = JObject.Parse(obj).GetValue("id").ToString();

      var transportAwaits = serializer.SecondaryWriteTransports.Select(t => t.WriteComplete()).ToList();
      transportAwaits.Add(localTransport.WriteComplete());

      await Task.WhenAll(transportAwaits);


      return hash;
    }

    /// <summary>
    /// Pushes and transports an object, as well as any of its detachable children, to all the transports provided.
    /// <para>This method is an integrated serialization and transportation step.</para>
    /// </summary>
    /// <param name="object">Base object to send</param>
    /// <param name="streamId">StreamId to send the objects to</param>
    /// <param name="client">Client to use</param>
    /// <param name="localTransport"></param>
    /// <param name="onProgressAction">An action that is invoked with a dictionary argument containing key value pairs of (process name, processed items).</param>
    /// <returns>The object's id (hash).</returns>
    public static async Task<string> Send(Base @object, string streamId, Client client, ITransport localTransport = null, Action<ConcurrentDictionary<string, int>> onProgressAction = null)
    {
      return await Send(@object, new List<string> { streamId }, new List<Client> { client }, localTransport, onProgressAction);
    }


    /// <summary>
    /// Serializes and transports a list of objects by first wrapping them into a commit.
    /// </summary>
    /// <param name="objects"></param>
    /// <param name="localTransport"></param>
    /// <param name="onProgressAction">An action that is invoked with a dictionary argument containing key value pairs of (process name, processed items).</param>
    /// <returns>The commit's id (hash).</returns>
    public static async Task<List<string>> Send(IEnumerable<Base> objects, ITransport localTransport = null, Action<ConcurrentDictionary<string, int>> onProgressAction = null)
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

      var obj = JsonConvert.SerializeObject(objects, settings);
      var res = JsonConvert.DeserializeObject<List<ObjectReference>>(obj);

      var transportAwaits = serializer.SecondaryWriteTransports.Select(t => t.WriteComplete()).ToList();
      transportAwaits.Add(localTransport.WriteComplete());

      await Task.WhenAll(transportAwaits);

      return res.Select(o => o.referencedId).ToList();
    }

 
    /// <summary>
    /// Pushes and transports a list of object, as well as any of their detachable children, to all the transports provided.
    /// <para>This method is an integrated serialization and transportation step.</para>
    /// </summary>
    /// <param name="objects">Base objects to send</param>
    /// <param name="streamIds">List of StreamIds to send the objects to</param>
    /// <param name="clients">List of Clients to use</param>
    /// <param name="localTransport"></param>
    /// <param name="onProgressAction">An action that is invoked with a dictionary argument containing key value pairs of (process name, processed items).</param>
    /// <returns>The object's id (hash).</returns>
    public static async Task<List<string>> Send(IEnumerable<Base> objects, IEnumerable<string> streamIds, IEnumerable<Client> clients, ITransport localTransport = null, Action<ConcurrentDictionary<string, int>> onProgressAction = null)
    {
      if (streamIds.Count() != clients.Count())
      {
        Log.CaptureAndThrow(new SpeckleException($"The number of streams and clients provided does not match."), SentryLevel.Error);
      }

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

     for (var i = 0; i < clients.Count(); i++)
      {
        var client = clients.ElementAt(i);
        var streamId = streamIds.ElementAt(i);
        serializer.SecondaryWriteTransports.Add(new RemoteTransport(client.ServerUrl, streamId, client.ApiToken)
        {
          LocalTransport = serializer.Transport,
          OnProgressAction = internalProgressAction
        });
      }

      var obj = JsonConvert.SerializeObject(objects, settings);
      var res = JsonConvert.DeserializeObject<List<ObjectReference>>(obj);

      var transportAwaits = serializer.SecondaryWriteTransports.Select(t => t.WriteComplete()).ToList();
      transportAwaits.Add(localTransport.WriteComplete());

      await Task.WhenAll(transportAwaits);

      return res.Select(o => o.referencedId).ToList();
    }

    /// <summary>
    /// Pushes and transports a list of object, as well as any of their detachable children, to the transport provided.
    /// <para>This method is an integrated serialization and transportation step.</para>
    /// </summary>
    /// <param name="objects">Base objects to send</param>
    /// <param name="streamId">StreamId to send the objects to</param>
    /// <param name="client">Client to use</param>
    /// <param name="localTransport"></param>
    /// <param name="onProgressAction">An action that is invoked with a dictionary argument containing key value pairs of (process name, processed items).</param>
    /// <returns>The object's id (hash).</returns>
    public static async Task<List<string>> Send(IEnumerable<Base> objects, string streamId, Client client, ITransport localTransport = null, Action<ConcurrentDictionary<string, int>> onProgressAction = null)
    {
      return await Send(objects, new List<string> { streamId }, new List<Client> { client }, localTransport, onProgressAction);
    }
      #endregion

    }
}
