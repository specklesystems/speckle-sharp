using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sentry;
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
    /// Sends an object via the provided transports. Defaults to the local cache. 
    /// </summary>
    /// <param name="object">The object you want to send.</param>
    /// <param name="transports">Where you want to send them.</param>
    /// <param name="useDefaultCache">Toggle for the default cache. If set to false, it will only send to the provided transports.</param>
    /// <param name="onProgressAction">Action that gets triggered on every progress tick (keeps track of all transports).</param>
    /// <returns>The id (hash) of the object.</returns>
    public static async Task<string> Send(Base @object, List<ITransport> transports = null, bool useDefaultCache = true, Action<ConcurrentDictionary<string, int>> onProgressAction = null)
    {
      return await Send(
        @object: @object,
        cancellationToken: CancellationToken.None,
        transports: transports,
        useDefaultCache: useDefaultCache,
        onProgressAction: onProgressAction
        );
    }

    /// <summary>
    /// Sends an object via the provided transports. Defaults to the local cache. 
    /// </summary>
    /// <param name="object">The object you want to send.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to send notice of cancellation.</param>
    /// <param name="transports">Where you want to send them.</param>
    /// <param name="useDefaultCache">Toggle for the default cache. If set to false, it will only send to the provided transports.</param>
    /// <param name="onProgressAction">Action that gets triggered on every progress tick (keeps track of all transports).</param>
    /// <returns>The id (hash) of the object.</returns>
    public static async Task<string> Send(Base @object, CancellationToken cancellationToken, List<ITransport> transports = null, bool useDefaultCache = true, Action<ConcurrentDictionary<string, int>> onProgressAction = null)
    {
      Log.AddBreadcrumb("Send");

      if (transports == null)
      {
        transports = new List<ITransport>();
      }

      if (transports.Count == 0 && useDefaultCache == false)
      {
        Log.CaptureAndThrow(new SpeckleException($"You need to provide at least one transport: cannot send with an empty transport list and no default cache."), SentryLevel.Error);
      }

      if (useDefaultCache)
      {
        transports.Insert(0, new SQLiteTransport());
      }

      var (serializer, settings) = GetSerializerInstance();

      var localProgressDict = new ConcurrentDictionary<string, int>();
      var internalProgressAction = Operations.GetInternalProgressAction(localProgressDict, onProgressAction);

      serializer.OnProgressAction = internalProgressAction;
      serializer.CancellationToken = cancellationToken;

      foreach (var t in transports)
      {
        t.OnProgressAction = internalProgressAction;
        t.CancellationToken = cancellationToken;
        serializer.WriteTransports.Add(t);
      }

      var obj = JsonConvert.SerializeObject(@object, settings);
      var hash = JObject.Parse(obj).GetValue("id").ToString();

      var transportAwaits = serializer.WriteTransports.Select(t => t.WriteComplete()).ToList();

      await Task.WhenAll(transportAwaits);

      return hash;
    }


    /// <summary>
    /// Sends a list of objects via the provided transports. Defaults to the local cache.
    /// <para>Note: If you're aiming to create a revision/commit afterwards, wrap your object list in a separate base object and use the other method.</para>
    /// </summary>
    /// <param name="objects">Base objects to send</param>
    /// <param name="onProgressAction">An action that is invoked with a dictionary argument containing key value pairs of (process name, processed items).</param>
    /// <returns>The object's id (hash).</returns>
    public static async Task<List<string>> Send(IEnumerable<Base> objects, List<ITransport> transports = null, bool useDefaultCache = true, Action<ConcurrentDictionary<string, int>> onProgressAction = null)
    {
      Log.AddBreadcrumb("Send Multiple");

      if (transports == null)
      {
        transports = new List<ITransport>();
      }

      if (transports.Count == 0 && useDefaultCache == false)
      {
        Log.CaptureAndThrow(new SpeckleException($"You need to provide at least one transport: cannot send with an empty transport list and no default cache."), SentryLevel.Error);
      }

      if (useDefaultCache)
      {
        transports.Insert(0, new SQLiteTransport());
      }

      var (serializer, settings) = GetSerializerInstance();

      var localProgressDict = new ConcurrentDictionary<string, int>();
      var internalProgressAction = GetInternalProgressAction(localProgressDict, onProgressAction);

      serializer.OnProgressAction = internalProgressAction;

      foreach (var t in transports)
      {
        t.OnProgressAction = internalProgressAction;
        serializer.WriteTransports.Add(t);
      }

      var obj = JsonConvert.SerializeObject(objects, settings);
      var res = JsonConvert.DeserializeObject<List<ObjectReference>>(obj);

      var transportAwaits = serializer.WriteTransports.Select(t => t.WriteComplete()).ToList();

      await Task.WhenAll(transportAwaits);

      return res.Select(o => o.referencedId).ToList();
    }


    #endregion

  }
}
