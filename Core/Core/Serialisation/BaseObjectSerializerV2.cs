using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.Core.Serialisation
{
  public class BaseObjectSerializerV2
  {
    /// <summary>
    /// Property that describes the type of the object.
    /// </summary>
    public string TypeDiscriminator = "speckle_type";

    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// The sync transport. This transport will be used synchronously. 
    /// </summary>
    public List<ITransport> WriteTransports { get; set; } = new List<ITransport>();

    public int TotalProcessedCount = 0;

    public Action<string, int> OnProgressAction { get; set; }

    public Action<string, Exception> OnErrorAction { get; set; }

    private Regex ChunkPropertyNameRegex = new Regex(@"^@\((\d*)\)");

    private Dictionary<string, List<(PropertyInfo, bool, bool, int)>> TypedPropertiesCache = new Dictionary<string, List<(PropertyInfo, bool, bool, int)>>();
    private List<Dictionary<string, int>> ParentClosures = new List<Dictionary<string, int>>();

    public BaseObjectSerializerV2()
    {

    }

    public string Serialize(Base baseObj)
    {
      Dictionary<string, object> converted = PreserializeObject(baseObj) as Dictionary<string, object>;
      String serialized = Dict2Json(converted);
      StoreObject(converted["id"] as string, serialized);
      return serialized;
    }

    public object PreserializeObject(object obj)
    {
      if (obj == null)
        return null;
      Type type = obj.GetType();
      if (type.IsPrimitive || obj is string)
        return obj;

      if (obj is Base)
      {
        // Complex enough to deserve its own function
        return PreserializeBase((Base)obj);
      }

      if (obj is IDictionary)
      {
        Dictionary<string, object> ret = new Dictionary<string, object>(((IDictionary)obj).Count);
        foreach (DictionaryEntry kvp in (IDictionary)obj)
          ret[kvp.Key.ToString()] = PreserializeObject(kvp.Value);
        return ret;
      }

      if (obj is IEnumerable)
      {
        List<object> ret;
        if (type is IList)
          ret = new List<object>(((IList)obj).Count);
        else
          ret = new List<object>();
        foreach (object element in ((IEnumerable)obj))
          ret.Add(PreserializeObject(element));
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

      throw new Exception("Unsupported value in serialization: " + type.ToString());
    }

    public object PreserializeBase(Base baseObj)
    {
      Dictionary<string, object> convertedBase = new Dictionary<string, object>();
      Dictionary<string, int> closure = new Dictionary<string, int>();
      ParentClosures.Add(closure);

      List<(PropertyInfo, bool, bool, int)> typedProperties = GetTypedPropertiesWithCache(baseObj);
      IEnumerable<string> dynamicProperties = baseObj.GetDynamicMembers();
      
      // propertyName -> (originalValue, isDetachable, isChunkable, chunkSize)
      Dictionary<string, (object, bool, bool, int)> allProperties = new Dictionary<string, (object, bool, bool, int)>();

      // Construct `allProperties`: Add typed properties
      foreach ((PropertyInfo propertyInfo, bool isDetachable, bool isChunkable, int chunkSize) in typedProperties)
      {
        object baseValue = propertyInfo.GetValue(baseObj);
        allProperties[propertyInfo.Name] = (baseValue, isDetachable, isChunkable, chunkSize);
      }

      // Construct `allProperties`: Add dynamic properties
      foreach (string propName in dynamicProperties)
      {
        object baseValue = baseObj[propName];
        bool isDetachable = propName.StartsWith("@");
        bool isChunkable = false;
        int chunkSize = 1000;

        if (ChunkPropertyNameRegex.IsMatch(propName))
        {
          var match = ChunkPropertyNameRegex.Match(propName);
          isChunkable = int.TryParse(match.Groups[match.Groups.Count - 1].Value, out chunkSize);
        }
        allProperties[propName] = (baseValue, isDetachable, isChunkable, chunkSize);
      }

      // Convert all properties
      foreach (var prop in allProperties)
      {
        object convertedValue = PreserializeBasePropertyValue(prop.Value.Item1, prop.Value.Item2, prop.Value.Item3, prop.Value.Item4);
        convertedBase[prop.Key] = convertedValue;
      }

      if (closure.Count > 0)
        convertedBase["__closure"] = closure;
      ParentClosures.RemoveAt(ParentClosures.Count - 1);

      return convertedBase;
    }
    
    private object PreserializeBasePropertyValue(object baseValue, bool isDetachable, bool isChunkable, int chunkSize)
    {
      object convertedValue = PreserializeObject(baseValue);

      // If there are no WriteTransports, keep everything attached.
      if (WriteTransports == null || WriteTransports.Count == 0)
        return convertedValue;

      if (convertedValue is List<object> && isChunkable) // TODO: Q: Chunkable implies detachable? (no reason to chunk if not detaching DataChunks?)
      {
        List<object> fullList = (List<object>)convertedValue;
        int chunkCount = fullList.Count % chunkSize == 0 ? fullList.Count / chunkSize : fullList.Count / chunkSize + 1;
        List<object> ret = new List<object>(chunkCount);

        for (int iChunk = 0; iChunk < chunkCount; iChunk++)
        {
          int startIdx = iChunk * chunkSize;
          int endIdx = (iChunk + 1) * chunkSize;
          if (endIdx > fullList.Count) endIdx = fullList.Count;

          // Construct a DataChunk object
          DataChunk crtChunk = new DataChunk();
          crtChunk.data = new List<object>(endIdx - startIdx);
          for (int i = startIdx; i < endIdx; i++)
            crtChunk.data.Add(fullList[i]);

          // Convert it to Dictionary
          Dictionary<string, object> convertedChunk = PreserializeObject(crtChunk) as Dictionary<string, object>;
          // Compute id and serialize
          string chunkJson = Dict2Json(convertedChunk);
          // Store in transports
          StoreObject(convertedChunk["id"] as string, chunkJson);
          // Make a reference to the detached chunk and store as a Dictionary in the returned list
          ObjectReference crtChunkRef = new ObjectReference() { referencedId = convertedChunk["id"] as string };
          object crtChunkRefConverted = PreserializeObject(crtChunkRef);
          ret.Add(crtChunkRefConverted);
          UpdateParentClosures(convertedChunk["id"] as string);
        }
        return ret;
      }
      
      if (convertedValue is List<object> && isDetachable)
      {
        List<object> fullList = (List<object>)convertedValue;
        List<object> ret = new List<object>(fullList.Count);
        foreach (object element in fullList)
        {
          if (!(element is Dictionary<string, object>))
          {
            ret.Add(element);
            continue;
          }
          string elementJson = Dict2Json((Dictionary<string, object>)element);
          // Store in transports
          string elementId = ((Dictionary<string, object>)element)["id"] as string;
          StoreObject(elementId, elementJson);
          // Make a reference to the detached chunk and store as a Dictionary in the returned list
          ObjectReference crtChunkRef = new ObjectReference() { referencedId = elementId };
          object crtChunkRefConverted = PreserializeObject(crtChunkRef);
          ret.Add(crtChunkRefConverted);
          UpdateParentClosures(elementId);
        }
        return ret;
      }

      if (convertedValue is Dictionary<string, object> && isDetachable)
      {
        Dictionary<string, object> convertedValueAsDict = (Dictionary<string, object>)convertedValue;
        // Compute id and serialize
        string json = Dict2Json(convertedValueAsDict);
        StoreObject(convertedValueAsDict["id"] as string, json);
        ObjectReference objRef = new ObjectReference() { referencedId = convertedValueAsDict["id"] as string };
        object objRefConverted = PreserializeObject(objRef);
        UpdateParentClosures(convertedValueAsDict["id"] as string);
        return objRefConverted;
      }

      return convertedValue;
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
      string serialized = JsonSerializer.Serialize<Dictionary<string, object>>(obj);
      string hash = Models.Utilities.hashString(serialized);
      return hash;
    }

    private string Dict2Json(Dictionary<string, object> obj)
    {
      // TODO: Q: Compute and add id to all Bases, or just the Detached ones?
      obj["id"] = ComputeId(obj);
      string serialized = JsonSerializer.Serialize<Dictionary<string, object>>(obj);
      return serialized;
    }

    private void StoreObject(string objectId, string objectJson)
    {
      foreach (var transport in WriteTransports)
      {
        transport.SaveObject(objectId, objectJson);
      }
    }


    // (propertyInfo, isDetachable, isChunkable, chunkSize)
    private List<(PropertyInfo, bool, bool, int)> GetTypedPropertiesWithCache(Base baseObj)
    {
      Type type = baseObj.GetType();
      IEnumerable<PropertyInfo> typedProperties = baseObj.GetInstanceMembers();

      if (TypedPropertiesCache.ContainsKey(type.FullName))
        return TypedPropertiesCache[type.FullName];

      List<(PropertyInfo, bool, bool, int)> ret = new List<(PropertyInfo, bool, bool, int)>();

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
        ret.Add((typedProperty, isDetachable, isChunkable, chunkSize));
      }

      TypedPropertiesCache[type.FullName] = ret;
      return ret;
    }
  }
}
