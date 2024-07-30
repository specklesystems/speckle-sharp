using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Context;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Serialisation;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Linq;

namespace Speckle.Core.Api;

public static partial class Operations
{
  private const string DEPRECATION_NOTICE = """
                                            This Send overload has been replaced by an overload with fewer function arguments.
                                            We are no longer supporting SerializerV1, OnErrorAction, or handling disposal of transports.
                                            Consider switching one of the other send overloads instead.
                                            This function will be kept around for several releases, but will eventually be removed.
                                            """;

  ///<inheritdoc cref="Send(Speckle.Core.Models.Base,System.Threading.CancellationToken,System.Collections.Generic.List{Speckle.Core.Transports.ITransport}?,bool,System.Action{System.Collections.Concurrent.ConcurrentDictionary{string,int}}?,System.Action{string,System.Exception}?,bool,Speckle.Core.Api.SerializerVersion)"/>
  [Obsolete("This overload has been deprecated along with serializer v1. Use other Send overloads instead.")]
  [SuppressMessage("Naming", "CA1720:Identifier contains type name")]
  public static Task<string> Send(Base @object) => Send(@object, CancellationToken.None);

  ///<inheritdoc cref="Send(Speckle.Core.Models.Base,System.Threading.CancellationToken,System.Collections.Generic.List{Speckle.Core.Transports.ITransport}?,bool,System.Action{System.Collections.Concurrent.ConcurrentDictionary{string,int}}?,System.Action{string,System.Exception}?,bool,Speckle.Core.Api.SerializerVersion)"/>
  [Obsolete("This overload has been deprecated along with serializer v1. Use other Send overloads instead.")]
  [SuppressMessage("Naming", "CA1720:Identifier contains type name")]
  public static Task<string> Send(
    Base @object,
    List<ITransport>? transports,
    bool useDefaultCache,
    Action<ConcurrentDictionary<string, int>>? onProgressAction,
    Action<string, Exception>? onErrorAction,
    bool disposeTransports,
    SerializerVersion serializerVersion = SerializerVersion.V2
  ) =>
    Send(
      @object,
      CancellationToken.None,
      transports,
      useDefaultCache,
      onProgressAction,
      onErrorAction,
      disposeTransports,
      serializerVersion
    );

  ///<inheritdoc cref="Send(Speckle.Core.Models.Base,System.Threading.CancellationToken,System.Collections.Generic.List{Speckle.Core.Transports.ITransport}?,bool,System.Action{System.Collections.Concurrent.ConcurrentDictionary{string,int}}?,System.Action{string,System.Exception}?,bool,Speckle.Core.Api.SerializerVersion)"/>
  [Obsolete("This overload has been deprecated along with serializer v1. Use other Send overloads instead.")]
  [SuppressMessage("Naming", "CA1720:Identifier contains type name")]
  public static Task<string> Send(
    Base @object,
    List<ITransport>? transports,
    bool useDefaultCache,
    bool disposeTransports,
    SerializerVersion serializerVersion = SerializerVersion.V2
  ) =>
    Send(
      @object,
      CancellationToken.None,
      transports,
      useDefaultCache,
      null,
      null,
      disposeTransports,
      serializerVersion
    );

  ///<inheritdoc cref="Send(Speckle.Core.Models.Base,System.Threading.CancellationToken,System.Collections.Generic.List{Speckle.Core.Transports.ITransport}?,bool,System.Action{System.Collections.Concurrent.ConcurrentDictionary{string,int}}?,System.Action{string,System.Exception}?,bool,Speckle.Core.Api.SerializerVersion)"/>
  [Obsolete("This overload has been deprecated along with serializer v1. Use other Send overloads instead.")]
  [SuppressMessage("Naming", "CA1720:Identifier contains type name")]
  public static Task<string> Send(
    Base @object,
    bool disposeTransports,
    SerializerVersion serializerVersion = SerializerVersion.V2
  ) => Send(@object, CancellationToken.None, null, true, null, null, disposeTransports, serializerVersion);

  /// <summary>
  /// Sends an object via the provided transports. Defaults to the local cache.
  /// </summary>
  /// <remarks>
  /// This overload is deprecated. You should consider using
  /// <br/><see cref="Send(Base, IReadOnlyCollection{ITransport}, Action{ConcurrentDictionary{string,int}}?, CancellationToken)"/>
  /// <br/>or
  /// <br/><see cref="Send(Base, ITransport, bool, Action{ConcurrentDictionary{string,int}}?,CancellationToken)"/>
  /// <br/>
  /// These new overloads no longer support <paramref name="serializerVersion"/> switching as v1 is now deprecated.
  /// <br/>
  /// We also no longer offer the option to <paramref name="disposeTransports"/>.
  /// You should instead handle disposal yourself
  /// using conventional mechanisms like the <c>using</c> keyword.<br/>
  /// <br/>
  /// This function overload will be kept around for several releases, but will eventually be removed.
  /// </remarks>
  /// <param name="object">The object you want to send.</param>
  /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to send notice of cancellation.</param>
  /// <param name="transports">Where you want to send them.</param>
  /// <param name="useDefaultCache">Toggle for the default cache. If set to false, it will only send to the provided transports.</param>
  /// <param name="onProgressAction">Action that gets triggered on every progress tick (keeps track of all transports).</param>
  /// <param name="onErrorAction">Use this to capture and handle any errors from within the transports.</param>
  /// <param name="disposeTransports"></param>
  /// <param name="serializerVersion"></param>
  /// <returns>The id (hash) of the object.</returns>
  [SuppressMessage("Naming", "CA1720:Identifier contains type name")]
  [Obsolete(DEPRECATION_NOTICE)]
  public static async Task<string> Send(
    Base @object,
    CancellationToken cancellationToken,
    List<ITransport>? transports = null,
    bool useDefaultCache = true,
    Action<ConcurrentDictionary<string, int>>? onProgressAction = null,
    Action<string, Exception>? onErrorAction = null,
    bool disposeTransports = false,
    SerializerVersion serializerVersion = SerializerVersion.V2
  )
  {
    transports ??= new List<ITransport>();
    using var sqLiteTransport = new SQLiteTransport { TransportName = "LC" };

    if (transports.Count == 0 && !useDefaultCache)
    {
      throw new ArgumentException(
        "You need to provide at least one transport: cannot send with an empty transport list and no default cache.",
        nameof(transports)
      );
    }

    if (useDefaultCache)
    {
      transports.Insert(0, sqLiteTransport);
    }

    var transportContext = transports.ToDictionary(t => t.TransportName, t => t.TransportContext);

    // make sure all logs in the operation have the proper context
    using (LogContext.PushProperty("transportContext", transportContext))
    using (LogContext.PushProperty("correlationId", Guid.NewGuid().ToString()))
    {
      var sendTimer = Stopwatch.StartNew();
      SpeckleLog.Logger.Information("Starting send operation");

      var internalProgressAction = GetInternalProgressAction(onProgressAction);

      BaseObjectSerializer? serializer = null;
      JsonSerializerSettings? settings = null;
      BaseObjectSerializerV2? serializerV2 = null;
      if (serializerVersion == SerializerVersion.V1)
      {
        (serializer, settings) = GetSerializerInstance();
        serializer.WriteTransports = transports;
        serializer!.OnProgressAction = internalProgressAction;
        serializer.CancellationToken = cancellationToken;
        serializer.OnErrorAction = onErrorAction;
      }
      else
      {
        serializerV2 = new BaseObjectSerializerV2(transports, internalProgressAction, cancellationToken);
      }

      foreach (var t in transports)
      {
        t.OnProgressAction = internalProgressAction;
        t.CancellationToken = cancellationToken;
        t.BeginWrite();
      }

      string obj;
      List<Task> transportAwaits;
      if (serializerVersion == SerializerVersion.V1)
      {
        obj = JsonConvert.SerializeObject(@object, settings);
        transportAwaits = serializer!.WriteTransports.Select(t => t.WriteComplete()).ToList();
      }
      else
      {
        obj = serializerV2!.Serialize(@object);
        transportAwaits = serializerV2.WriteTransports.Select(t => t.WriteComplete()).ToList();
      }

      if (cancellationToken.IsCancellationRequested)
      {
        SpeckleLog.Logger.Information(
          "Send operation cancelled after {elapsed} seconds",
          sendTimer.Elapsed.TotalSeconds
        );
        cancellationToken.ThrowIfCancellationRequested();
      }

      await Task.WhenAll(transportAwaits).ConfigureAwait(false);

      foreach (var t in transports)
      {
        t.EndWrite();
        if (useDefaultCache && t is SQLiteTransport lc && lc.TransportName == "LC")
        {
          lc.Dispose();
          continue;
        }
        if (disposeTransports && t is IDisposable disp)
        {
          disp.Dispose();
        }
      }

      if (cancellationToken.IsCancellationRequested)
      {
        SpeckleLog.Logger.Information("Send operation cancelled after {elapsed}", sendTimer.Elapsed.TotalSeconds);
        cancellationToken.ThrowIfCancellationRequested();
      }

      var idToken = JObject.Parse(obj).GetValue("id");
      if (idToken == null)
      {
        throw new SpeckleException("Failed to get id of serialized object");
      }

      var hash = idToken.ToString();

      sendTimer.Stop();
      SpeckleLog.Logger
        .ForContext("transportElapsedBreakdown", transports.ToDictionary(t => t.TransportName, t => t.Elapsed))
        .ForContext("note", "the elapsed summary doesn't need to add up to the total elapsed... Threading magic...")
        .ForContext("serializerElapsed", serializerV2?.Elapsed)
        .Information(
          "Finished sending {objectCount} objects after {elapsed}, result {objectId}",
          transports.Max(t => t.SavedObjectCount),
          sendTimer.Elapsed.TotalSeconds,
          hash
        );
      return hash;
    }
  }
}
