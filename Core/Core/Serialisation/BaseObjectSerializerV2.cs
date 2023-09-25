using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.DoubleNumerics;
using System.Reflection;
using System.Text.RegularExpressions;
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
  private bool _busy;

  private List<Dictionary<string, int>> ParentClosures = new();

  private HashSet<object> ParentObjects = new();

  /// <summary>
  /// Property that describes the type of the object.
  /// </summary>
  public string TypeDiscriminator = "speckle_type";

  private Dictionary<string, List<(PropertyInfo, PropertyAttributeInfo)>> TypedPropertiesCache = new();

  public CancellationToken CancellationToken { get; set; }

  /// <summary>
  /// The sync transport. This transport will be used synchronously.
  /// </summary>
  public List<ITransport> WriteTransports { get; set; } = new();

  public Action<string, int> OnProgressAction { get; set; }

  public Action<string, Exception> OnErrorAction { get; set; }

  // duration diagnostic stuff
  public TimeSpan Elapsed => _stopwatch.Elapsed;

  public string Serialize(Base baseObj)
  {
    if (_busy)
      throw new Exception(
        "A serializer instance can serialize only 1 object at a time. Consider creating multiple serializer instances"
      );
    try
    {
      _stopwatch.Start();
      _busy = true;
      Dictionary<string, object> converted = PreserializeObject(baseObj, true) as Dictionary<string, object>;
      string serialized = Dict2Json(converted);
      StoreObject(converted["id"] as string, serialized);
      return serialized;
    }
    finally
    {
      ParentClosures = new List<Dictionary<string, int>>(); // cleanup in case of exceptions
      ParentObjects = new HashSet<object>();
      _busy = false;
      _stopwatch.Stop();
    }
  }

  // `Preserialize` means transforming all objects into the final form that will appear in json, with basic .net objects
  // (primitives, lists and dictionaries with string keys)
  public object PreserializeObject(
    object obj,
    bool computeClosures = false,
    PropertyAttributeInfo inheritedDetachInfo = default
  )
  {
    CancellationToken.ThrowIfCancellationRequested();

    if (obj == null)
      return null;

    Type type = obj.GetType();

    if (type.IsPrimitive || obj is string)
      return obj;

    if (obj is Base b)
      // Complex enough to deserve its own function
      return PreserializeBase(b, computeClosures, inheritedDetachInfo);

    if (obj is IDictionary d)
    {
      Dictionary<string, object> ret = new(d.Count);
      foreach (DictionaryEntry kvp in d)
      {
        object converted = PreserializeObject(kvp.Value, inheritedDetachInfo: inheritedDetachInfo);
        if (converted != null)
          ret[kvp.Key.ToString()] = converted;
      }
      return ret;
    }
    //TODO: handle IReadonlyDictionary

    if (obj is IEnumerable e)
    {
      List<object> ret;
      if (e is IList list)
        ret = new List<object>(list.Count);
      else
        ret = new List<object>();
      foreach (object element in e)
        ret.Add(PreserializeObject(element, inheritedDetachInfo: inheritedDetachInfo));
      return ret;
    }

    if (obj is ObjectReference r)
    {
      Dictionary<string, object> ret = new();
      ret["speckle_type"] = r.speckle_type;
      ret["referencedId"] = r.referencedId;
      return ret;
    }

    if (obj is Enum)
      return (int)obj;

    // Support for simple types
    if (obj is Guid g)
      return g.ToString();
    if (obj is Color c)
      return c.ToArgb();
    if (obj is DateTime t)
      return t.ToString("o", CultureInfo.InvariantCulture);
    if (obj is Matrix4x4 md)
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
    if (obj is System.Numerics.Matrix4x4 ms) //BACKWARDS COMPATIBILITY: matrix4x4 changed from System.Numerics float to System.DoubleNumerics double in release 2.16
    {
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
    }

    throw new Exception("Unsupported value in serialization: " + type);
  }

  public object PreserializeBase(
    Base baseObj,
    bool computeClosures = false,
    PropertyAttributeInfo inheritedDetachInfo = default
  )
  {
    // handle circular references
    if (ParentObjects.Contains(baseObj))
      return null;
    ParentObjects.Add(baseObj);

    Dictionary<string, object> convertedBase = new();
    Dictionary<string, int> closure = new();
    if (computeClosures || inheritedDetachInfo.IsDetachable || baseObj is Blob)
      ParentClosures.Add(closure);

    List<(PropertyInfo, PropertyAttributeInfo)> typedProperties = GetTypedPropertiesWithCache(baseObj);
    IEnumerable<string> dynamicProperties = baseObj.GetDynamicMembers();

    // propertyName -> (originalValue, isDetachable, isChunkable, chunkSize)
    Dictionary<string, (object, PropertyAttributeInfo)> allProperties = new();

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
        continue;
      object baseValue = baseObj[propName];
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
      object convertedValue = PreserializeBasePropertyValue(prop.Value.Item1, prop.Value.Item2);

      if (
        convertedValue == null
        && prop.Value.Item2.JsonPropertyInfo != null
        && prop.Value.Item2.JsonPropertyInfo.NullValueHandling == NullValueHandling.Ignore
      )
        continue;

      convertedBase[prop.Key] = convertedValue;
    }

    if (baseObj is Blob blob)
      convertedBase["id"] = blob.id;
    else
      convertedBase["id"] = ComputeId(convertedBase);

    if (closure.Count > 0)
      convertedBase["__closure"] = closure;
    if (computeClosures || inheritedDetachInfo.IsDetachable || baseObj is Blob)
      ParentClosures.RemoveAt(ParentClosures.Count - 1);

    ParentObjects.Remove(baseObj);

    if (baseObj is Blob myBlob)
    {
      StoreBlob(myBlob);
      UpdateParentClosures($"blob:{convertedBase["id"]}");
      return convertedBase;
    }

    if (inheritedDetachInfo.IsDetachable && WriteTransports != null && WriteTransports.Count > 0)
    {
      string json = Dict2Json(convertedBase);
      StoreObject(convertedBase["id"] as string, json);
      ObjectReference objRef = new() { referencedId = convertedBase["id"] as string };
      object objRefConverted = PreserializeObject(objRef);
      UpdateParentClosures(convertedBase["id"] as string);
      OnProgressAction?.Invoke("S", 1);
      return objRefConverted;
    }

    return convertedBase;
  }

  private object PreserializeBasePropertyValue(object baseValue, PropertyAttributeInfo detachInfo)
  {
    bool computeClosuresForChild =
      (detachInfo.IsDetachable || detachInfo.IsChunkable) && WriteTransports != null && WriteTransports.Count > 0;

    // If there are no WriteTransports, keep everything attached.
    if (WriteTransports == null || WriteTransports.Count == 0)
      return PreserializeObject(baseValue, inheritedDetachInfo: detachInfo);

    if (baseValue is IEnumerable && detachInfo.IsChunkable)
    {
      List<object> chunks = new();
      DataChunk crtChunk = new();
      crtChunk.data = new List<object>(detachInfo.ChunkSize);
      foreach (object element in (IEnumerable)baseValue)
      {
        crtChunk.data.Add(element);
        if (crtChunk.data.Count >= detachInfo.ChunkSize)
        {
          chunks.Add(crtChunk);
          crtChunk = new DataChunk();
          crtChunk.data = new List<object>(detachInfo.ChunkSize);
        }
      }
      if (crtChunk.data.Count > 0)
        chunks.Add(crtChunk);
      return PreserializeObject(chunks, inheritedDetachInfo: new PropertyAttributeInfo(true, false, 0, null));
    }

    return PreserializeObject(baseValue, inheritedDetachInfo: detachInfo);
  }

  private void UpdateParentClosures(string objectId)
  {
    for (int parentLevel = 0; parentLevel < ParentClosures.Count; parentLevel++)
    {
      int childDepth = ParentClosures.Count - parentLevel;
      if (!ParentClosures[parentLevel].ContainsKey(objectId))
        ParentClosures[parentLevel][objectId] = childDepth;
      ParentClosures[parentLevel][objectId] = Math.Min(ParentClosures[parentLevel][objectId], childDepth);
    }
  }

  private static string ComputeId(Dictionary<string, object> obj)
  {
    string serialized = JsonConvert.SerializeObject(obj);
    string hash = Utilities.HashString(serialized);
    return hash;
  }

  private static string Dict2Json(Dictionary<string, object> obj)
  {
    string serialized = JsonConvert.SerializeObject(obj);
    return serialized;
  }

  private void StoreObject(string objectId, string objectJson)
  {
    if (WriteTransports == null)
      return;
    _stopwatch.Stop();
    foreach (var transport in WriteTransports)
      transport.SaveObject(objectId, objectJson);
    _stopwatch.Start();
  }

  private void StoreBlob(Blob obj)
  {
    if (WriteTransports == null)
      return;
    bool hasBlobTransport = false;

    _stopwatch.Stop();

    foreach (var transport in WriteTransports)
      if (transport is IBlobCapableTransport blobTransport)
      {
        hasBlobTransport = true;
        blobTransport.SaveBlob(obj);
      }

    _stopwatch.Start();
    if (!hasBlobTransport)
      throw new InvalidOperationException(
        "Object tree contains a Blob (file), but the serialiser has no blob saving capable transports."
      );
  }

  // (propertyInfo, isDetachable, isChunkable, chunkSize, JsonPropertyAttribute)
  private List<(PropertyInfo, PropertyAttributeInfo)> GetTypedPropertiesWithCache(Base baseObj)
  {
    Type type = baseObj.GetType();
    IEnumerable<PropertyInfo> typedProperties = baseObj.GetInstanceMembers();

    if (TypedPropertiesCache.ContainsKey(type.FullName))
      return TypedPropertiesCache[type.FullName];

    List<(PropertyInfo, PropertyAttributeInfo)> ret = new();

    foreach (PropertyInfo typedProperty in typedProperties)
    {
      if (typedProperty.Name.StartsWith("__") || typedProperty.Name == "id")
        continue;

      // Check JsonIgnore like this to cover both Newtonsoft JsonIgnore and System.Text.Json JsonIgnore
      // TODO: replace JsonIgnore from newtonsoft with JsonIgnore from Sys, and check this more properly.
      bool jsonIgnore = false;
      foreach (object attr in typedProperty.GetCustomAttributes(true))
        if (attr.GetType().Name.Contains("JsonIgnore"))
        {
          jsonIgnore = true;
          break;
        }
      if (jsonIgnore)
        continue;

      object baseValue = typedProperty.GetValue(baseObj);

      List<DetachProperty> detachableAttributes = typedProperty.GetCustomAttributes<DetachProperty>(true).ToList();
      List<Chunkable> chunkableAttributes = typedProperty.GetCustomAttributes<Chunkable>(true).ToList();
      bool isDetachable = detachableAttributes.Count > 0 && detachableAttributes[0].Detachable;
      bool isChunkable = chunkableAttributes.Count > 0;
      int chunkSize = isChunkable ? chunkableAttributes[0].MaxObjCountPerChunk : 1000;
      JsonPropertyAttribute jsonPropertyAttribute = typedProperty.GetCustomAttribute<JsonPropertyAttribute>();
      ret.Add((typedProperty, new PropertyAttributeInfo(isDetachable, isChunkable, chunkSize, jsonPropertyAttribute)));
    }

    TypedPropertiesCache[type.FullName] = ret;
    return ret;
  }

  public struct PropertyAttributeInfo
  {
    public PropertyAttributeInfo(
      bool isDetachable,
      bool isChunkable,
      int chunkSize,
      JsonPropertyAttribute jsonPropertyAttribute
    )
    {
      IsDetachable = isDetachable || isChunkable;
      IsChunkable = isChunkable;
      ChunkSize = chunkSize;
      JsonPropertyInfo = jsonPropertyAttribute;
    }

    public bool IsDetachable;
    public bool IsChunkable;
    public int ChunkSize;
    public JsonPropertyAttribute JsonPropertyInfo;
  }
}
