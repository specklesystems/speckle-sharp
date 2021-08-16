using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.Core.Serialisation
{
  class BaseObjectSerializerV2
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
        return converted as Base;
      }
    }

    private object ConvertJsonElement(JsonElement doc)
    {
      switch(doc.ValueKind)
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
            retList.Add(convertedValue);
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
      Type type = SerializationUtilities.GetType(dictObj[TypeDiscriminator] as String);
      Base baseObj = Activator.CreateInstance(type) as Base;

      dictObj.Remove(TypeDiscriminator);
      dictObj.Remove("__closure");

      foreach (KeyValuePair<string, object> entry in dictObj)
      {
        
        // TODO: Assign it to `baseObj` property
      }
      ((dynamic)baseObj).dataDict = dictObj;

      return baseObj;
    }
  }
}
