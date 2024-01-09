using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Speckle.Core.Serialisation;
using Speckle.Newtonsoft.Json;

namespace Speckle.Core.Transports;

public static class TransportHelpers
{
  public static string CopyObjectAndChildrenSync(
    string id,
    ITransport sourceTransport,
    ITransport targetTransport,
    Action<int>? onTotalChildrenCountKnown,
    CancellationToken cancellationToken
  )
  {
    if (string.IsNullOrEmpty(id))
    {
      throw new ArgumentException("Cannot copy object with empty id", nameof(id));
    }

    cancellationToken.ThrowIfCancellationRequested();

    var parent = sourceTransport.GetObject(id);
    if (parent is null)
    {
      throw new TransportException(
        $"Requested id {id} was not found within this transport {sourceTransport.TransportName}"
      );
    }

    targetTransport.SaveObject(id, parent);

    var closures = GetClosureTable(parent);

    onTotalChildrenCountKnown?.Invoke(closures?.Count ?? 0);

    if (closures is not null)
    {
      int i = 0;
      foreach (var kvp in closures)
      {
        cancellationToken.ThrowIfCancellationRequested();

        var child = sourceTransport.GetObject(kvp.Key);
        if (child is null)
        {
          throw new TransportException(
            $"Closure id {kvp.Key} was not found within this transport {sourceTransport.TransportName}"
          );
        }

        targetTransport.SaveObject(kvp.Key, child);
        sourceTransport.OnProgressAction?.Invoke($"{sourceTransport}", i++);
      }
    }

    return parent;
  }

  /// <param name="objString">The Json object</param>
  /// <returns>The closure table</returns>
  /// <exception cref="SpeckleDeserializeException">Failed to deserialize the object into <see cref="Placeholder"/></exception>
  internal static Dictionary<string, int>? GetClosureTable(string objString) //TODO: Unit Test
  {
    var partial = JsonConvert.DeserializeObject<Placeholder>(objString);

    if (partial is null)
    {
      throw new SpeckleDeserializeException($"Failed to deserialize {nameof(objString)} into {nameof(Placeholder)}");
    }

    return partial.__closure;
  }

  [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Deserialization target for DTO")]
  internal sealed class Placeholder
  {
    public Dictionary<string, int>? __closure { get; set; }
  }
}
