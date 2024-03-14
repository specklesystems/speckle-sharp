#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Context;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Serialisation;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json.Linq;

namespace DUI3.Operations;

/// <summary>
/// NOTE: Contains copy pasted code from the OG Send operations in Core (the non-obsolete ones). 
/// </summary>
public static class SendHelper
{
  
  /// <summary>
  /// IMPORTANT: Copy pasted function from Operations.Send in Core, but this time returning the converted references from the serializer.
  /// Sends a Speckle Object to the provided <paramref name="transport"/> and (optionally) the default local cache
  /// </summary>
  /// <remarks/>
  /// <inheritdoc cref="Send(Base, IReadOnlyCollection{ITransport}, Action{ConcurrentDictionary{string, int}}?, CancellationToken)"/>
  /// <param name="useDefaultCache">When <see langword="true"/>, an additional <see cref="SQLiteTransport"/> will be included</param>
  /// <exception cref="ArgumentNullException">The <paramref name="transport"/> or <paramref name="value"/> was <see langword="null"/></exception>
  /// <example><code>
  /// using ServerTransport destination = new(account, streamId);
  /// string objectId = await Send(mySpeckleObject, destination, true);
  /// </code></example>
  public static async Task<(string rootObjId, Dictionary<string,ObjectReference> convertedReferences)> Send(
    Base value,
    ITransport transport,
    bool useDefaultCache,
    Action<ConcurrentDictionary<string, int>>? onProgressAction = null,
    CancellationToken cancellationToken = default
  )
  {
    if (transport is null)
    {
      throw new ArgumentNullException(nameof(transport), "Expected a transport to be explicitly specified");
    }

    List<ITransport> transports = new() { transport };
    using SQLiteTransport? localCache = useDefaultCache ? new SQLiteTransport { TransportName = "LC" } : null;
    if (localCache is not null)
    {
      transports.Add(localCache);
    }

    return await Send(value, transports, onProgressAction, cancellationToken).ConfigureAwait(false);
  }
  
  /// <summary>
  /// IMPORTANT: Copy pasted function from Operations.Send in Core, but this time returning the converted references from the serializer.
  /// It's marked as private as DUI3 only uses the one above.
  /// Note that this should be structured better in the future - this is here to minimise core changes coming from DUI3. 
  /// </summary>
  /// <param name="value"></param>
  /// <param name="transports"></param>
  /// <param name="onProgressAction"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <exception cref="ArgumentNullException"></exception>
  /// <exception cref="ArgumentException"></exception>
  /// <exception cref="SpeckleException"></exception>
  private static async Task<(string rootObjId, Dictionary<string, ObjectReference> convertedReferences)> Send(
    Base value,
    IReadOnlyCollection<ITransport> transports,
    Action<ConcurrentDictionary<string, int>>? onProgressAction = null,
    CancellationToken cancellationToken = default)
  {
    if (value is null)
    {
      throw new ArgumentNullException(nameof(value));
    }

    if (transports.Count == 0)
    {
      throw new ArgumentException("Expected at least on transport to be specified", nameof(transports));
    }

    var transportContext = transports.ToDictionary(t => t.TransportName, t => t.TransportContext);

    // make sure all logs in the operation have the proper context
    using (LogContext.PushProperty("transportContext", transportContext))
    using (LogContext.PushProperty("correlationId", Guid.NewGuid().ToString()))
    {
      var sendTimer = Stopwatch.StartNew();
      SpeckleLog.Logger.Information("Starting send operation");

      var internalProgressAction = GetInternalProgressAction(onProgressAction);

      BaseObjectSerializerV2 serializerV2 = new(transports, internalProgressAction, trackDetachedChildren: true, cancellationToken);

      foreach (var t in transports)
      {
        t.OnProgressAction = internalProgressAction;
        t.CancellationToken = cancellationToken;
        t.BeginWrite();
      }

      (string rootObjId, Dictionary<string,ObjectReference>) serializerReturnValue;
      try
      {
        serializerReturnValue = await SerializerSend(value, serializerV2, cancellationToken).ConfigureAwait(false);
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        SpeckleLog.Logger.Information(
          ex,
          "Send operation failed after {elapsed} seconds",
          sendTimer.Elapsed.TotalSeconds
        );
        if (ex is OperationCanceledException or SpeckleException)
        {
          throw;
        }

        throw new SpeckleException("Send operation was unsuccessful", ex);
      }
      finally
      {
        foreach (var t in transports)
        {
          t.EndWrite();
        }
      }

      sendTimer.Stop();
      SpeckleLog.Logger
        .ForContext("transportElapsedBreakdown", transports.ToDictionary(t => t.TransportName, t => t.Elapsed))
        .ForContext("note", "the elapsed summary doesn't need to add up to the total elapsed... Threading magic...")
        .ForContext("serializerElapsed", serializerV2.Elapsed)
        .Information(
          "Finished sending {objectCount} objects after {elapsed}, result {objectId}",
          transports.Max(t => t.SavedObjectCount),
          sendTimer.Elapsed.TotalSeconds,
          serializerReturnValue.rootObjId
        );
      return serializerReturnValue;
    }
  }
  
  internal static async Task<(string rootObjectId, Dictionary<string,ObjectReference> convertedReferences)> SerializerSend(
    Base value,
    BaseObjectSerializerV2 serializer,
    CancellationToken cancellationToken = default
  )
  {
    string obj = serializer.Serialize(value);
    Task[] transportAwaits = serializer.WriteTransports.Select(t => t.WriteComplete()).ToArray();

    cancellationToken.ThrowIfCancellationRequested();

    await Task.WhenAll(transportAwaits).ConfigureAwait(false);

    var parsed = JObject.Parse(obj);
    JToken? idToken = parsed.GetValue("id");
    
    if (idToken == null)
    {
      throw new SpeckleException("Failed to get id of serialized object");
    }
    
    return (idToken.ToString(), serializer.ObjectReferences);
  }
  
  /// <summary>
  /// Factory for progress actions used internally inside send and receive methods.
  /// </summary>
  /// <param name="onProgressAction"></param>
  /// <returns></returns>
  private static Action<string, int>? GetInternalProgressAction(
    Action<ConcurrentDictionary<string, int>>? onProgressAction
  )
  {
    if (onProgressAction is null)
    {
      return null;
    }

    var localProgressDict = new ConcurrentDictionary<string, int>();

    return (name, processed) =>
    {
      if (!localProgressDict.TryAdd(name, processed))
      {
        localProgressDict[name] += processed;
      }

      onProgressAction.Invoke(localProgressDict);
    };
  }
  
}
