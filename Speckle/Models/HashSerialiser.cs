using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Speckle.Models
{
  /// <summary>
  /// Serialiser used to hash objects - quick and dirty. 
  /// </summary>
  internal class HashSerialiser : JsonConverter
  {
    public override bool CanWrite => true;
    public override bool CanRead => false;
    public HashSet<string> ParsedObjects = new HashSet<string>();

    public override bool CanConvert(Type objectType) => true;

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
    {
      throw new NotImplementedException();
    }

    List<int> Lineage = new List<int>();

    Dictionary<int, int> DepthReferenceTracker = new Dictionary<int, int>();

    void IncrementDepthReference(int hashCode)
    {
      foreach (var hc in Lineage)
      {
        if (!DepthReferenceTracker.ContainsKey(hc)) DepthReferenceTracker[hc] = 1;
        else DepthReferenceTracker[hc]++;
      }
    }

    public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
    {
      if (value == null)
        return;

      if (value is Base)
      {
        var obj = value as Base;

        Lineage.Add(value.GetHashCode());
        IncrementDepthReference(value.GetHashCode());

        var jo = new JObject();
        var propertyNames = obj.GetDynamicMemberNames();
        var contract = (JsonDynamicContract)serializer.ContractResolver.ResolveContract(value.GetType());

        foreach (var prop in propertyNames)
        {
          if (prop == "hash") continue;
          if (prop == "subRefCount") continue;
          if (prop.StartsWith("__", StringComparison.CurrentCulture)) continue;

          // Ignore properties decorated with [JsonIgnore].
          var property = contract?.Properties.GetClosestMatchProperty(prop);
          if (property != null && property.Ignored) continue;

          // Ignore properties flagged with [IgnoreAtHashing]
          if (property != null)
          {
            var attrs = property.AttributeProvider.GetAttributes(typeof(ExcludeHashing), true);
            if (attrs.Count > 0) continue;
          }

          object propValue = obj[prop];
          if (propValue == null) continue;

          jo.Add(prop, JToken.FromObject(propValue, serializer));
        }

        jo.WriteTo(writer);

        Lineage.Remove(Lineage.Count - 1);
        //if (DepthReferenceTracker.ContainsKey(value.GetHashCode()))
        //  ((Base)value).__subRefCount = DepthReferenceTracker[value.GetHashCode()];
        return;
      }

      var type = value.GetType();

      // List handling
      if (typeof(IEnumerable).IsAssignableFrom(type) && !typeof(IDictionary).IsAssignableFrom(type) && type != typeof(string))
      {
        JArray arr = new JArray();
        foreach (var arrValue in ((IEnumerable)value))
        {
          arr.Add(JToken.FromObject(arrValue, serializer));
        }
        arr.WriteTo(writer);
        return;
      }

      // Dictionary handling
      if (typeof(IDictionary).IsAssignableFrom(type))
      {
        var dict = value as IDictionary;
        var dictJo = new JObject();
        foreach (DictionaryEntry kvp in dict)
        {
          dictJo.Add(kvp.Key.ToString(), JToken.FromObject(kvp.Value, serializer));
        }
        dictJo.WriteTo(writer);
        return;
      }

      var t = JToken.FromObject(value); // bypasses this converter
      t.WriteTo(writer);
    }
  }
}