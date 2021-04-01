using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sentry;
using Sentry;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Linq;

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
    /// <param name="onErrorAction">Use this to capture and handle any errors from within the transports.</param>
    /// <returns>The id (hash) of the object.</returns>
    public static Task<string> Send(Base @object, List<ITransport> transports = null, bool useDefaultCache = true, Action<ConcurrentDictionary<string, int>> onProgressAction = null, Action<string, Exception> onErrorAction = null)
    {
      return Send(
        @object,
        CancellationToken.None,
        transports,
        useDefaultCache,
        onProgressAction,
        onErrorAction
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
    /// <param name="onErrorAction">Use this to capture and handle any errors from within the transports.</param>
    /// <returns>The id (hash) of the object.</returns>
    public static async Task<string> Send(Base @object, CancellationToken cancellationToken, List<ITransport> transports = null, bool useDefaultCache = true, Action<ConcurrentDictionary<string, int>> onProgressAction = null, Action<string, Exception> onErrorAction = null)
    {
      Log.AddBreadcrumb("Send");

      if (transports == null)
      {
        transports = new List<ITransport>();
      }

      if (transports.Count == 0 && useDefaultCache == false)
      {
        throw new SpeckleException($"You need to provide at least one transport: cannot send with an empty transport list and no default cache.", level : SentryLevel.Error);
      }

      if (useDefaultCache)
      {
        transports.Insert(0, new SQLiteTransport() { TransportName = "LC" });
      }

      var(serializer, settings) = GetSerializerInstance();

      var localProgressDict = new ConcurrentDictionary<string, int>();
      var internalProgressAction = Operations.GetInternalProgressAction(localProgressDict, onProgressAction);

      serializer.OnProgressAction = internalProgressAction;
      serializer.CancellationToken = cancellationToken;
      serializer.OnErrorAction = onErrorAction;

      foreach (var t in transports)
      {
        t.OnProgressAction = internalProgressAction;
        t.CancellationToken = cancellationToken;
        t.OnErrorAction = onErrorAction;
        t.BeginWrite();

        serializer.WriteTransports.Add(t);
      }

      var obj = JsonConvert.SerializeObject(@object, settings);

      var transportAwaits = serializer.WriteTransports.Select(t => t.WriteComplete()).ToList();

      if (cancellationToken.IsCancellationRequested)return null;

      await Task.WhenAll(transportAwaits).ConfigureAwait(false);

      foreach (var t in transports)
      {
        t.EndWrite();
      }

      if (cancellationToken.IsCancellationRequested)return null;

      var hash = JObject.Parse(obj).GetValue("id").ToString();
      return hash;
    }

    #endregion

  }
}
