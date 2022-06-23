using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Linq;

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
        List<(string, int)> closureList = new List<(string, int)>();
        JObject doc1 = JObject.Parse(rootObjectJson);

        if (!doc1.ContainsKey("__closure"))
          return new List<(string, int)>();
        foreach(JToken prop in doc1["__closure"])
        {
          string childId = ((JProperty)prop).Name;
          int childMinDepth = (int)((JProperty)prop).Value;
          closureList.Add((childId, childMinDepth));
        }
        return closureList;
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
      // Apparently this automatically parses DateTimes in strings if it matches the format:
      // JObject doc1 = JObject.Parse(objectJson); 
      
      // This is equivalent code that doesn't parse datetimes:
      JObject doc1;
      using (JsonReader reader = new JsonTextReader(new System.IO.StringReader(objectJson)))
      {
        reader.DateParseHandling = DateParseHandling.None;
        doc1 = JObject.Load(reader);
      }


      object converted = ConvertJsonElement(doc1);
      lock (CallbackLock)
      {
        OnProgressAction?.Invoke("DS", 1);
      }
      return converted;
    }

    public object ConvertJsonElement(JToken doc)
    {
      if (CancellationToken.IsCancellationRequested)
      {
        return null; // Check for cancellation
      }

      switch(doc.Type)
      {
        case JTokenType.Undefined:
        case JTokenType.Null:
        case JTokenType.None:
          return null;
        case JTokenType.Boolean:
          return (bool)doc;
        case JTokenType.Integer:
          return (long)doc;
        case JTokenType.Float:
          return (double)doc;
        case JTokenType.String:
          return (string)doc;
        case JTokenType.Date:
          return (DateTime)doc;
        case JTokenType.Array:
          JArray docAsArray = (JArray)doc;
          List<object> jsonList = new List<object>(docAsArray.Count);
          int retListCount = 0;
          foreach(JToken value in docAsArray)
          {
            object convertedValue = ConvertJsonElement(value);
            retListCount += (convertedValue is DataChunk) ? ((DataChunk)convertedValue).data.Count : 1;
            jsonList.Add(convertedValue);
          }

          List<object> retList = new List<object>(retListCount);
          foreach (object jsonObj in jsonList)
          {
            if (jsonObj is DataChunk)
              retList.AddRange(((DataChunk)jsonObj).data);
            else
              retList.Add(jsonObj);
          }

          return retList;
        case JTokenType.Object:
          Dictionary<string, object> dict = new Dictionary<string, object>();

          foreach (JToken propJToken in doc)
          {
            JProperty prop = (JProperty)propJToken;
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
              try
              {
                deserialized = ((Task<object>)deserialized).Result;
              }
              catch(AggregateException aggregateEx)
              {
                throw aggregateEx.InnerException;
              }
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
        default:
          throw new Exception("Json value not supported: " + doc.Type.ToString());
      }
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
          if (entry.Value == null)
          {
            // Check for JsonProperty(NullValueHandling = NullValueHandling.Ignore) attribute
            JsonPropertyAttribute attr = property.GetCustomAttribute<JsonPropertyAttribute>(true);
            if (attr != null && attr.NullValueHandling == NullValueHandling.Ignore)
              continue;
          }

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
