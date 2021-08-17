using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
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
    public ITransport ReadTransport { get; set; }

    public int TotalProcessedCount = 0;

    public Action<string, int> OnProgressAction { get; set; }

    public Action<string, Exception> OnErrorAction { get; set; }


    public Base Deserialize(String objectJson)
    {
      using (JsonDocument doc = JsonDocument.Parse(objectJson))
      {
        object converted = ConvertJsonElement(doc.RootElement);
        OnProgressAction?.Invoke("DS", 1);
        return converted as Base;
      }
    }

    private object ConvertJsonElement(JsonElement doc)
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
          return doc.GetDouble();

        case JsonValueKind.Array:
          List<object> retList = new List<object>(doc.GetArrayLength());
          foreach (JsonElement value in doc.EnumerateArray())
          {
            object convertedValue = ConvertJsonElement(value);

            if (convertedValue is DataChunk)
            {
              retList.AddRange(((DataChunk)convertedValue).data);
            }
            else
            {
              retList.Add(convertedValue);
            }
          }
          return retList;

        case JsonValueKind.Object:
          Dictionary<string, object> dict = new Dictionary<string, object>();
          foreach (JsonProperty prop in doc.EnumerateObject())
            dict[prop.Name] = ConvertJsonElement(prop.Value);

          if (!dict.ContainsKey(TypeDiscriminator))
            return dict;

          if ((dict[TypeDiscriminator] as String) == "reference" && dict.ContainsKey("referencedId"))
          {
            string objectJson = ReadTransport.GetObject(dict["referencedId"] as String);
            return Deserialize(objectJson);
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
            CallSiteCache.SetValue(entry.Key, baseObj, entry.Value);
          }
        }
        else
        {
          // No writable property with this name
          CallSiteCache.SetValue(entry.Key, baseObj, entry.Value);
        }
      }

      return baseObj;
    }

  }
}
