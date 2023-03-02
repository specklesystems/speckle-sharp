using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;

namespace Speckle.Core.Serialisation
{

  public class BaseObjectSerializerV2
  {
    public struct PropertyAttributeInfo
    {
      public PropertyAttributeInfo(bool isDetachable, bool isChunkable, int chunkSize, JsonPropertyAttribute jsonPropertyAttribute)
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

    /// <summary>
    /// Property that describes the type of the object.
    /// </summary>
    public string TypeDiscriminator = "speckle_type";

    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// The sync transport. This transport will be used synchronously. 
    /// </summary>
    public List<ITransport> WriteTransports { get; set; } = new List<ITransport>();

    public Action<string, int> OnProgressAction { get; set; }

    public Action<string, Exception> OnErrorAction { get; set; }

    private Regex ChunkPropertyNameRegex = new Regex(@"^@\((\d*)\)");

    private Dictionary<string, List<(PropertyInfo, PropertyAttributeInfo)>> TypedPropertiesCache = new Dictionary<string, List<(PropertyInfo, PropertyAttributeInfo)>>();
    private List<Dictionary<string, int>> ParentClosures = new List<Dictionary<string, int>>();
    private bool Busy = false;

    private HashSet<object> ParentObjects = new HashSet<object>();

    public BaseObjectSerializerV2()
    {

    }

    public string Serialize(Base baseObj)
    {
      if (Busy)
        throw new Exception("A serializer instance can serialize only 1 object at a time. Consider creating multiple serializer instances");
      try
      {
        Busy = true;
        Dictionary<string, object> converted = PreserializeObject(baseObj, true) as Dictionary<string, object>;
        String serialized = Dict2Json(converted);
        StoreObject(converted["id"] as string, serialized);
        return serialized;
      }
      finally
      {
        ParentClosures = new List<Dictionary<string, int>>(); // cleanup in case of exceptions
        ParentObjects = new HashSet<object>();
        Busy = false;
      }
    }

    // `Preserialize` means transforming all objects into the final form that will appear in json, with basic .net objects
    // (primitives, lists and dictionaries with string keys)
    public object PreserializeObject(object obj, bool computeClosures = false, PropertyAttributeInfo inheritedDetachInfo = default(PropertyAttributeInfo))
    {
      // handle null objects and also check for cancelation
      if (obj == null || CancellationToken.IsCancellationRequested)
        return null;

      Type type = obj.GetType();

      if (type.IsPrimitive || obj is string)
        return obj;

      if (obj is Base)
      {
        // Complex enough to deserve its own function
        return PreserializeBase((Base)obj, computeClosures, inheritedDetachInfo);
      }

      if (obj is IDictionary)
      {
        Dictionary<string, object> ret = new Dictionary<string, object>(((IDictionary)obj).Count);
        foreach (DictionaryEntry kvp in (IDictionary)obj)
        {
          object converted = PreserializeObject(kvp.Value, inheritedDetachInfo: inheritedDetachInfo);
          if (converted != null)
            ret[kvp.Key.ToString()] = converted;
        }
        return ret;
      }

      if (obj is IEnumerable)
      {
        List<object> ret;
        if (obj is IList)
          ret = new List<object>(((IList)obj).Count);
        else if (obj is Array)
          ret = new List<object>(((Array)obj).Length);
        else
          ret = new List<object>();
        foreach (object element in ((IEnumerable)obj))
          ret.Add(PreserializeObject(element, inheritedDetachInfo: inheritedDetachInfo));
        return ret;
      }

      if (obj is ObjectReference)
      {
        Dictionary<string, object> ret = new Dictionary<string, object>();
        ret["speckle_type"] = ((ObjectReference)obj).speckle_type;
        ret["referencedId"] = ((ObjectReference)obj).referencedId;
        return ret;
      }

      if (obj is Enum)
      {
        return (int)obj;
      }

      // Support for simple types
      if (obj is Guid)
      {
        return ((Guid)obj).ToString();
      }
      if (obj is System.Drawing.Color)
      {
        return ((System.Drawing.Color)obj).ToArgb();
      }
      if (obj is DateTime)
      {
        return ((DateTime)obj).ToString("o", System.Globalization.CultureInfo.InvariantCulture);
      }
      if (obj is Matrix4x4 m)
      {
        return new List<float>()
        {
          m.M11, m.M12, m.M13, m.M14,
          m.M21, m.M22, m.M23, m.M24,
          m.M31, m.M32, m.M33, m.M34,
          m.M41, m.M42, m.M43, m.M44
        };
      }

      throw new Exception("Unsupported value in serialization: " + type.ToString());
    }

    public object PreserializeBase(Base baseObj, bool computeClosures = false, PropertyAttributeInfo inheritedDetachInfo = default(PropertyAttributeInfo))
    {
      // handle circular references
      if (ParentObjects.Contains(baseObj))
        return null;
      ParentObjects.Add(baseObj);

      Dictionary<string, object> convertedBase = new Dictionary<string, object>();
      Dictionary<string, int> closure = new Dictionary<string, int>();
      if (computeClosures || inheritedDetachInfo.IsDetachable || baseObj is Blob)
        ParentClosures.Add(closure);

      List<(PropertyInfo, PropertyAttributeInfo)> typedProperties = GetTypedPropertiesWithCache(baseObj);
      IEnumerable<string> dynamicProperties = baseObj.GetDynamicMembers();

      // propertyName -> (originalValue, isDetachable, isChunkable, chunkSize)
      Dictionary<string, (object, PropertyAttributeInfo)> allProperties = new Dictionary<string, (object, PropertyAttributeInfo)>();

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

        if (ChunkPropertyNameRegex.IsMatch(propName))
        {
          var match = ChunkPropertyNameRegex.Match(propName);
          isChunkable = int.TryParse(match.Groups[match.Groups.Count - 1].Value, out chunkSize);
        }
        allProperties[propName] = (baseValue, new PropertyAttributeInfo(isDetachable, isChunkable, chunkSize, null));
      }

      // Convert all properties
      foreach (var prop in allProperties)
      {
        object convertedValue = PreserializeBasePropertyValue(prop.Value.Item1, prop.Value.Item2);

        if (convertedValue == null && prop.Value.Item2.JsonPropertyInfo != null && prop.Value.Item2.JsonPropertyInfo.NullValueHandling == NullValueHandling.Ignore)
          continue;

        convertedBase[prop.Key] = convertedValue;
      }

      if (baseObj is Blob blob)
      {
        convertedBase["id"] = blob.id;
      }
      else
      {
        convertedBase["id"] = ComputeId(convertedBase);
      }

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
        ObjectReference objRef = new ObjectReference() { referencedId = convertedBase["id"] as string };
        object objRefConverted = PreserializeObject(objRef);
        UpdateParentClosures(convertedBase["id"] as string);
        OnProgressAction?.Invoke("S", 1);
        return objRefConverted;
      }

      return convertedBase;
    }

    private object PreserializeBasePropertyValue(object baseValue, PropertyAttributeInfo detachInfo)
    {
      bool computeClosuresForChild = (detachInfo.IsDetachable || detachInfo.IsChunkable) && WriteTransports != null && WriteTransports.Count > 0;

      // If there are no WriteTransports, keep everything attached.
      if (WriteTransports == null || WriteTransports.Count == 0)
        return PreserializeObject(baseValue, inheritedDetachInfo: detachInfo);

      if (baseValue is IEnumerable && detachInfo.IsChunkable)
      {
        List<object> chunks = new List<object>();
        DataChunk crtChunk = new DataChunk();
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

    private string ComputeId(Dictionary<string, object> obj)
    {
      string serialized = JsonConvert.SerializeObject(obj);
      string hash = Models.Utilities.hashString(serialized);
      return hash;
    }

    private string Dict2Json(Dictionary<string, object> obj)
    {
      string serialized = JsonConvert.SerializeObject(obj);
      return serialized;
    }

    private void StoreObject(string objectId, string objectJson)
    {
      if (WriteTransports == null)
        return;
      foreach (var transport in WriteTransports)
      {
        transport.SaveObject(objectId, objectJson);
      }
    }

    private void StoreBlob(Blob obj)
    {
      if (WriteTransports == null)
        return;
      bool hasBlobTransport = false;

      foreach (var transport in WriteTransports)
      {
        if (transport is IBlobCapableTransport blobTransport)
        {
          hasBlobTransport = true;
          blobTransport.SaveBlob(obj);
        }
      }

      if (!hasBlobTransport)
        throw new Exception("Object tree contains a Blob (file), but the serialiser has no blob saving capable transports.");
    }

    // (propertyInfo, isDetachable, isChunkable, chunkSize, JsonPropertyAttribute)
    private List<(PropertyInfo, PropertyAttributeInfo)> GetTypedPropertiesWithCache(Base baseObj)
    {
      Type type = baseObj.GetType();
      IEnumerable<PropertyInfo> typedProperties = baseObj.GetInstanceMembers();

      if (TypedPropertiesCache.ContainsKey(type.FullName))
        return TypedPropertiesCache[type.FullName];

      List<(PropertyInfo, PropertyAttributeInfo)> ret = new List<(PropertyInfo, PropertyAttributeInfo)>();

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
  }
}
