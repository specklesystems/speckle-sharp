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
  public class BaseObjectDeserializerV2
  {
    /// <summary>
    /// Property that describes the type of the object.
    /// </summary>
    public string TypeDiscriminator = "speckle_type";

    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// The sync transport. This transport will be used synchronously. 
    /// </summary>
    public ITransport ReadTransport { get; set; }

    public int TotalProcessedCount = 0;

    public Action<string, int> OnProgressAction { get; set; }

    public Action<string, Exception> OnErrorAction { get; set; }

    private DeserializationWorkerThreads WorkerThreads;
    private bool Busy = false;
    // id -> Base if already deserialized or id -> Task<object> if was handled by a bg thread
    private Dictionary<string, object> DeserializedObjects;
    private object CallbackLock = new object();

    private Regex ChunkPropertyNameRegex = new Regex(@"^@\((\d*)\)");

    public BaseObjectDeserializerV2()
    {

    }

    public Base Deserialize(String rootObjectJson)
    {
      if (Busy)
        throw new Exception("A deserializer instance can deserialize only 1 object at a time. Consider creating multiple deserializer instances");
      try
      {
        Busy = true;
        DeserializedObjects = new Dictionary<string, object>();
        WorkerThreads = new DeserializationWorkerThreads(this);
        WorkerThreads.Start();

        List<(string, int)> closures = GetClosures(rootObjectJson);
        closures.Sort((a, b) => b.Item2.CompareTo(a.Item2));
        foreach (var closure in closures)
        {
          string objId = closure.Item1;
          string objJson = ReadTransport.GetObject(objId);
          object deserializedOrPromise = DeserializeTransportObjectProxy(objJson);
          lock (DeserializedObjects)
          {
            DeserializedObjects[objId] = deserializedOrPromise;
          }
        }

        object ret = DeserializeTransportObject(rootObjectJson);
        return ret as Base;
      }
      finally
      {
        DeserializedObjects = null;
        WorkerThreads.Dispose();
        WorkerThreads = null;
        Busy = false;
      }
    }

    private List<(string, int)> GetClosures(string rootObjectJson)
    {
      try
      {
        using (JsonDocument doc = JsonDocument.Parse(rootObjectJson))
        {
          List<(string, int)> closureList = new List<(string, int)>();
          JsonElement closures = doc.RootElement.GetProperty("__closure");
          foreach (JsonProperty prop in closures.EnumerateObject())
          {
            closureList.Add((prop.Name, prop.Value.GetInt32()));
          }
          return closureList;
        }
      }
      catch
      {
        return new List<(string, int)>();
      }
    }

    private object DeserializeTransportObjectProxy(String objectJson)
    {
      // Try background work
      Task<object> bgResult = WorkerThreads.TryStartTask(WorkerThreadTaskType.Deserialize, objectJson);
      if (bgResult != null)
        return bgResult;

      // Sync
      return DeserializeTransportObject(objectJson);
    }

    public object DeserializeTransportObject(String objectJson)
    {
      using (JsonDocument doc = JsonDocument.Parse(objectJson))
      {
        object converted = ConvertJsonElement(doc.RootElement);
        lock (CallbackLock)
        {
          OnProgressAction?.Invoke("DS", 1);
        }
        return converted;
      }
    }

    public object ConvertJsonElement(JsonElement doc)
    {
      if (CancellationToken.IsCancellationRequested)
      {
        return null; // Check for cancellation
      }

      switch (doc.ValueKind)
      {
        case JsonValueKind.Undefined:
        case JsonValueKind.Null:
          return null;

        case JsonValueKind.True:
          return true;
        case JsonValueKind.False:
          return false;

        case JsonValueKind.String:
          return doc.GetString();

        case JsonValueKind.Number:
          long i64value;
          if (doc.TryGetInt64(out i64value))
            return i64value;
          return doc.GetDouble();

        case JsonValueKind.Array:
          List<object> jsonList = new List<object>(doc.GetArrayLength());
          int retListCount = 0;
          foreach (JsonElement value in doc.EnumerateArray())
          {
            object convertedValue = ConvertJsonElement(value);
            retListCount += (convertedValue is DataChunk) ? ((DataChunk)convertedValue).data.Count : 1;
            jsonList.Add(convertedValue);
          }

          List<object> retList = new List<object>(retListCount);
          foreach(object jsonObj in jsonList)
          {
            if (jsonObj is DataChunk)
              retList.AddRange(((DataChunk)jsonObj).data);
            else
              retList.Add(jsonObj);
          }

          return retList;

        case JsonValueKind.Object:
          Dictionary<string, object> dict = new Dictionary<string, object>();

          foreach (JsonProperty prop in doc.EnumerateObject())
          {
            if (prop.Name == "__closure")
              continue;
            dict[prop.Name] = ConvertJsonElement(prop.Value);
          }

          if (!dict.ContainsKey(TypeDiscriminator))
            return dict;

          if ((dict[TypeDiscriminator] as String) == "reference" && dict.ContainsKey("referencedId"))
          {
            string objId = dict["referencedId"] as String;
            object deserialized = null;
            lock (DeserializedObjects)
            {
              if (DeserializedObjects.ContainsKey(objId))
                deserialized = DeserializedObjects[objId];
            }
            if (deserialized != null && deserialized is Task<object>)
            {
              deserialized = ((Task<object>)deserialized).Result;
              lock (DeserializedObjects)
              {
                DeserializedObjects[objId] = deserialized;
              }
            }

            if (deserialized != null)
              return deserialized;

            // This reference was not already deserialized. Do it now in sync mode
            string objectJson = ReadTransport.GetObject(objId);
            deserialized = DeserializeTransportObject(objectJson);
            lock (DeserializedObjects)
            {
              DeserializedObjects[objId] = deserialized;
            }
            return deserialized;
          }

          return Dict2Base(dict);
      }
      return null;
    }

    private Base Dict2Base(Dictionary<string, object> dictObj)
    {
      String typeName = dictObj[TypeDiscriminator] as String;
      Type type = SerializationUtilities.GetType(typeName);
      Base baseObj = Activator.CreateInstance(type) as Base;

      dictObj.Remove(TypeDiscriminator);
      dictObj.Remove("__closure");

      Dictionary<string, PropertyInfo> staticProperties = SerializationUtilities.GetTypePropeties(typeName);
      List<MethodInfo> onDeserializedCallbacks = SerializationUtilities.GetOnDeserializedCallbacks(typeName);

      foreach (KeyValuePair<string, object> entry in dictObj)
      {
        string lowerPropertyName = entry.Key.ToLower();
        if (staticProperties.ContainsKey(lowerPropertyName) && staticProperties[lowerPropertyName].CanWrite)
        {
          PropertyInfo property = staticProperties[lowerPropertyName];
          Type targetValueType = property.PropertyType;
          object convertedValue;
          bool conversionOk = ValueConverter.ConvertValue(targetValueType, entry.Value, out convertedValue);
          if (conversionOk)
          {
            property.SetValue(baseObj, convertedValue);
          }
          else
          {
            // Cannot convert the value in the json to the static property type
            throw new Exception(String.Format("Cannot deserialize {0} to {1}", entry.Value.GetType().FullName, targetValueType.FullName));
          }
        }
        else
        {
          // No writable property with this name
          CallSiteCache.SetValue(entry.Key, baseObj, entry.Value);
        }
      }

      foreach(MethodInfo onDeserialized in onDeserializedCallbacks)
      {
        onDeserialized.Invoke(baseObj, new object[] { null });
      }

      return baseObj;
    }
  }
}
