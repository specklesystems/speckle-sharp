using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.DoubleNumerics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;
using Utilities = Speckle.Core.Models.Utilities;

namespace Speckle.Core.Serialisation;

public class BaseObjectSerializerV2
{
  private readonly Stopwatch _stopwatch = new();
  private volatile bool _isBusy;
  private List<Dictionary<string, int>> _parentClosures = new();
  private HashSet<object> _parentObjects = new();
  private readonly Dictionary<string, List<(PropertyInfo, PropertyAttributeInfo)>> _typedPropertiesCache = new();
  private readonly Action<string, int>? _onProgressAction;

  /// <summary>The sync transport. This transport will be used synchronously.</summary>
  public IReadOnlyCollection<ITransport> WriteTransports { get; }

  public CancellationToken CancellationToken { get; set; }

  /// <summary>The current total elapsed time spent serializing</summary>
  public TimeSpan Elapsed => _stopwatch.Elapsed;

  public BaseObjectSerializerV2()
    : this(Array.Empty<ITransport>()) { }

  public BaseObjectSerializerV2(
    IReadOnlyCollection<ITransport> writeTransports,
    Action<string, int>? onProgressAction = null,
    CancellationToken cancellationToken = default
  )
  {
    WriteTransports = writeTransports;
    _onProgressAction = onProgressAction;
    CancellationToken = cancellationToken;
  }

  /// <param name="baseObj">The object to serialize</param>
  /// <returns>The serialized JSON</returns>
  /// <exception cref="InvalidOperationException">The serializer is busy (already serializing an object)</exception>
  /// <exception cref="TransportException">Failed to save object in one or more <see cref="WriteTransports"/></exception>
  /// <exception cref="SpeckleSerializeException">Failed to extract (pre-serialize) properties from the <paramref name="baseObj"/></exception>
  /// <exception cref="OperationCanceledException">One or more <see cref="WriteTransports"/>'s cancellation token requested cancel</exception>
  public string Serialize(Base baseObj)
  {
    if (_isBusy)
    {
      throw new InvalidOperationException(
        "A serializer instance can serialize only 1 object at a time. Consider creating multiple serializer instances"
      );
    }

    try
    {
      _stopwatch.Start();
      _isBusy = true;
      IDictionary<string, object?> converted;
      try
      {
        converted = PreserializeBase(baseObj, true)!;
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        throw new SpeckleSerializeException($"Failed to extract (pre-serialize) properties from the {baseObj}", ex);
      }
      string serialized = Dict2Json(converted);
      StoreObject((string)converted["id"]!, serialized);
      return serialized;
    }
    finally
    {
      _parentClosures = new List<Dictionary<string, int>>(); // cleanup in case of exceptions
      _parentObjects = new HashSet<object>();
      _isBusy = false;
      _stopwatch.Stop();
    }
  }

  // `Preserialize` means transforming all objects into the final form that will appear in json, with basic .net objects
  // (primitives, lists and dictionaries with string keys)
  public object? PreserializeObject(
    object? obj,
    bool computeClosures = false,
    PropertyAttributeInfo inheritedDetachInfo = default
  )
  {
    CancellationToken.ThrowIfCancellationRequested();

    if (obj == null)
    {
      return null;
    }

    if (obj.GetType().IsPrimitive || obj is string)
    {
      return obj;
    }

    switch (obj)
    {
      // Complex enough to deserve its own function
      case Base b:
        return PreserializeBase(b, computeClosures, inheritedDetachInfo);
      case IDictionary d:
      {
        Dictionary<string, object> ret = new(d.Count);
        foreach (DictionaryEntry kvp in d)
        {
          object? converted = PreserializeObject(kvp.Value, inheritedDetachInfo: inheritedDetachInfo);
          if (converted != null)
          {
            ret[kvp.Key.ToString()] = converted;
          }
        }
        return ret;
      }
      case IEnumerable e:
      {
        //TODO: handle IReadonlyDictionary
        int preSize = (e is IList list) ? list.Count : 0;

        List<object?> ret = new(preSize);

        foreach (object? element in e)
        {
          ret.Add(PreserializeObject(element, inheritedDetachInfo: inheritedDetachInfo));
        }

        return ret;
      }
      case ObjectReference r:
      {
        Dictionary<string, object> ret = new() { ["speckle_type"] = r.speckle_type, ["referencedId"] = r.referencedId };
        return ret;
      }
      case Enum:
        return (int)obj;
      // Support for simple types
      case Guid g:
        return g.ToString();
      case Color c:
        return c.ToArgb();
      case DateTime t:
        return t.ToString("o", CultureInfo.InvariantCulture);
      case Matrix4x4 md:
        return new List<double>
        {
          md.M11,
          md.M12,
          md.M13,
          md.M14,
          md.M21,
          md.M22,
          md.M23,
          md.M24,
          md.M31,
          md.M32,
          md.M33,
          md.M34,
          md.M41,
          md.M42,
          md.M43,
          md.M44
        };
      //BACKWARDS COMPATIBILITY: matrix4x4 changed from System.Numerics float to System.DoubleNumerics double in release 2.16
      case System.Numerics.Matrix4x4 ms:
        SpeckleLog.Logger.Warning(
          "This kept for backwards compatibility, no one should be using {this}",
          "BaseObjectSerializerV2 serialize System.Numerics.Matrix4x4"
        );
        return new List<double>
        {
          ms.M11,
          ms.M12,
          ms.M13,
          ms.M14,
          ms.M21,
          ms.M22,
          ms.M23,
          ms.M24,
          ms.M31,
          ms.M32,
          ms.M33,
          ms.M34,
          ms.M41,
          ms.M42,
          ms.M43,
          ms.M44
        };
      default:
        throw new ArgumentException($"Unsupported value in serialization: {obj.GetType()}");
    }
  }

  public IDictionary<string, object?>? PreserializeBase(
    Base baseObj,
    bool computeClosures = false,
    PropertyAttributeInfo inheritedDetachInfo = default
  )
  {
    // handle circular references
    bool alreadySerialized = !_parentObjects.Add(baseObj);
    if (alreadySerialized)
    {
      return null;
    }

    Dictionary<string, object?> convertedBase = new();
    Dictionary<string, int> closure = new();
    if (computeClosures || inheritedDetachInfo.IsDetachable || baseObj is Blob)
    {
      _parentClosures.Add(closure);
    }

    List<(PropertyInfo, PropertyAttributeInfo)> typedProperties = GetTypedPropertiesWithCache(baseObj);
    IEnumerable<string> dynamicProperties = baseObj.GetDynamicMembers();

    // propertyName -> (originalValue, isDetachable, isChunkable, chunkSize)
    Dictionary<string, (object?, PropertyAttributeInfo)> allProperties = new();

    // Construct `allProperties`: Add typed properties
    foreach ((PropertyInfo propertyInfo, PropertyAttributeInfo detachInfo) in typedProperties)
    {
      object baseValue = propertyInfo.GetValue(baseObj);
      allProperties[propertyInfo.Name] = (baseValue, detachInfo);
    }

    // Construct `allProperties`: Add dynamic properties
    foreach (string propName in dynamicProperties)
    {
      if (propName.StartsWith("__"))
      {
        continue;
      }

      object? baseValue = baseObj[propName];
      bool isDetachable = propName.StartsWith("@");
      bool isChunkable = false;
      int chunkSize = 1000;

      if (Constants.ChunkPropertyNameRegex.IsMatch(propName))
      {
        var match = Constants.ChunkPropertyNameRegex.Match(propName);
        isChunkable = int.TryParse(match.Groups[match.Groups.Count - 1].Value, out chunkSize);
      }
      allProperties[propName] = (baseValue, new PropertyAttributeInfo(isDetachable, isChunkable, chunkSize, null));
    }

    // Convert all properties
    foreach (var prop in allProperties)
    {
      object? convertedValue = PreserializeBasePropertyValue(prop.Value.Item1, prop.Value.Item2);

      if (
        convertedValue == null
        && prop.Value.Item2.JsonPropertyInfo is { NullValueHandling: NullValueHandling.Ignore }
      )
      {
        continue;
      }

      convertedBase[prop.Key] = convertedValue;
    }

    convertedBase["id"] = baseObj is Blob blob ? blob.id : ComputeId(convertedBase);

    if (closure.Count > 0)
    {
      convertedBase["__closure"] = closure;
    }

    if (computeClosures || inheritedDetachInfo.IsDetachable || baseObj is Blob)
    {
      _parentClosures.RemoveAt(_parentClosures.Count - 1);
    }

    _parentObjects.Remove(baseObj);

    if (baseObj is Blob myBlob)
    {
      StoreBlob(myBlob);
      UpdateParentClosures($"blob:{convertedBase["id"]}");
      return convertedBase;
    }

    if (inheritedDetachInfo.IsDetachable && WriteTransports.Count > 0)
    {
      string json = Dict2Json(convertedBase);
      string id = (string)convertedBase["id"]!;
      StoreObject(id, json);
      ObjectReference objRef = new() { referencedId = id };
      var objRefConverted = (IDictionary<string, object?>?)PreserializeObject(objRef);
      UpdateParentClosures(id);
      _onProgressAction?.Invoke("S", 1);
      return objRefConverted;
    }

    return convertedBase;
  }

  private object? PreserializeBasePropertyValue(object? baseValue, PropertyAttributeInfo detachInfo)
  {
    // If there are no WriteTransports, keep everything attached.
    if (WriteTransports.Count == 0)
    {
      return PreserializeObject(baseValue, inheritedDetachInfo: detachInfo);
    }

    if (baseValue is IEnumerable chunkableCollection && detachInfo.IsChunkable)
    {
      List<object> chunks = new();
      DataChunk crtChunk = new() { data = new List<object>(detachInfo.ChunkSize) };

      foreach (object element in chunkableCollection)
      {
        crtChunk.data.Add(element);
        if (crtChunk.data.Count >= detachInfo.ChunkSize)
        {
          chunks.Add(crtChunk);
          crtChunk = new DataChunk { data = new List<object>(detachInfo.ChunkSize) };
        }
      }

      if (crtChunk.data.Count > 0)
      {
        chunks.Add(crtChunk);
      }

      return PreserializeObject(chunks, inheritedDetachInfo: new PropertyAttributeInfo(true, false, 0, null));
    }

    return PreserializeObject(baseValue, inheritedDetachInfo: detachInfo);
  }

  private void UpdateParentClosures(string objectId)
  {
    for (int parentLevel = 0; parentLevel < _parentClosures.Count; parentLevel++)
    {
      int childDepth = _parentClosures.Count - parentLevel;
      if (!_parentClosures[parentLevel].TryGetValue(objectId, out int currentValue))
      {
        currentValue = childDepth;
      }

      _parentClosures[parentLevel][objectId] = Math.Min(currentValue, childDepth);
    }
  }

  private static string ComputeId(IDictionary<string, object?> obj)
  {
    string serialized = JsonConvert.SerializeObject(obj);
    string hash = Utilities.HashString(serialized);
    return hash;
  }

  private static string Dict2Json(IDictionary<string, object?>? obj)
  {
    string serialized = JsonConvert.SerializeObject(obj);
    return serialized;
  }

  private void StoreObject(string objectId, string objectJson)
  {
    _stopwatch.Stop();
    foreach (var transport in WriteTransports)
    {
      transport.SaveObject(objectId, objectJson);
    }

    _stopwatch.Start();
  }

  private void StoreBlob(Blob obj)
  {
    bool hasBlobTransport = false;

    _stopwatch.Stop();

    foreach (var transport in WriteTransports)
    {
      if (transport is IBlobCapableTransport blobTransport)
      {
        hasBlobTransport = true;
        blobTransport.SaveBlob(obj);
      }
    }

    _stopwatch.Start();
    if (!hasBlobTransport)
    {
      throw new InvalidOperationException(
        "Object tree contains a Blob (file), but the serializer has no blob saving capable transports."
      );
    }
  }

  // (propertyInfo, isDetachable, isChunkable, chunkSize, JsonPropertyAttribute)
  private List<(PropertyInfo, PropertyAttributeInfo)> GetTypedPropertiesWithCache(Base baseObj)
  {
    Type type = baseObj.GetType();
    IEnumerable<PropertyInfo> typedProperties = baseObj.GetInstanceMembers();

    if (_typedPropertiesCache.TryGetValue(type.FullName, out List<(PropertyInfo, PropertyAttributeInfo)>? cached))
    {
      return cached;
    }

    List<(PropertyInfo, PropertyAttributeInfo)> ret = new();

    foreach (PropertyInfo typedProperty in typedProperties)
    {
      if (typedProperty.Name.StartsWith("__") || typedProperty.Name == "id")
      {
        continue;
      }

      // Check JsonIgnore like this to cover both Newtonsoft JsonIgnore and System.Text.Json JsonIgnore
      // TODO: replace JsonIgnore from newtonsoft with JsonIgnore from Sys, and check this more properly.
      bool jsonIgnore = false;
      foreach (object attr in typedProperty.GetCustomAttributes(true))
      {
        if (attr.GetType().Name.Contains("JsonIgnore"))
        {
          jsonIgnore = true;
          break;
        }
      }
      if (jsonIgnore)
      {
        continue;
      }

      _ = typedProperty.GetValue(baseObj);

      List<DetachProperty> detachableAttributes = typedProperty.GetCustomAttributes<DetachProperty>(true).ToList();
      List<Chunkable> chunkableAttributes = typedProperty.GetCustomAttributes<Chunkable>(true).ToList();
      bool isDetachable = detachableAttributes.Count > 0 && detachableAttributes[0].Detachable;
      bool isChunkable = chunkableAttributes.Count > 0;
      int chunkSize = isChunkable ? chunkableAttributes[0].MaxObjCountPerChunk : 1000;
      JsonPropertyAttribute? jsonPropertyAttribute = typedProperty.GetCustomAttribute<JsonPropertyAttribute>();
      ret.Add((typedProperty, new PropertyAttributeInfo(isDetachable, isChunkable, chunkSize, jsonPropertyAttribute)));
    }

    _typedPropertiesCache[type.FullName] = ret;
    return ret;
  }

  public readonly struct PropertyAttributeInfo
  {
    public PropertyAttributeInfo(
      bool isDetachable,
      bool isChunkable,
      int chunkSize,
      JsonPropertyAttribute? jsonPropertyAttribute
    )
    {
      IsDetachable = isDetachable || isChunkable;
      IsChunkable = isChunkable;
      ChunkSize = chunkSize;
      JsonPropertyInfo = jsonPropertyAttribute;
    }

    public readonly bool IsDetachable;
    public readonly bool IsChunkable;
    public readonly int ChunkSize;
    public readonly JsonPropertyAttribute? JsonPropertyInfo;
  }

  [Obsolete("OnErrorAction unused, serializer will throw exceptions instead")]
  public Action<string, Exception>? OnErrorAction { get; set; }

  [Obsolete("Set via constructor instead", true)]
  public Action<string, int>? OnProgressAction
  {
    get => _onProgressAction;
    set => _ = value;
  }
}
