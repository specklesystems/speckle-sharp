using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Speckle.Models;

namespace Speckle.Serialisation
{
  public class BaseObjectSerializer : Newtonsoft.Json.JsonConverter
  {
    public BaseObjectSerializer()
    {
    }

    public override bool CanConvert(Type objectType) => true;

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      throw new NotImplementedException();
    }

    #region Write Json

    public Dictionary<string, string> ObjectBucket;

    List<bool> DetachLineage = new List<bool>();
    List<string> Lineage = new List<string>();
    Dictionary<string, HashSet<string>> ReferenceTracker = new Dictionary<string, HashSet<string>>();
    HashSet<string> Parsed = new HashSet<string>();
    string CurrentParentObjectHash = "";

    void TrackReferenceInTree(string refId)
    {
      var path = "";
      for (int i = Lineage.Count - 1; i >= 0; i--)
      {
        var parent = Lineage[i];
        path = parent + "." + path;

        if (!ReferenceTracker.ContainsKey(parent)) ReferenceTracker[parent] = new HashSet<string>();

        if (i == Lineage.Count - 1)
          ReferenceTracker[parent].Add(parent + "." + refId);
        else
          ReferenceTracker[parent].Add(path + refId);
      }
    }

    public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
    {
      if (value == null) return;

      if (value is Base && !(value is Reference))
      {
        var obj = value as Base;
        CurrentParentObjectHash = obj.hash;

        if (Parsed.Contains(CurrentParentObjectHash))
          return;

        // Append to lineage tracker
        Lineage.Add(CurrentParentObjectHash);

        var jo = new JObject();
        var propertyNames = obj.GetDynamicMemberNames();

        var contract = (JsonDynamicContract)serializer.ContractResolver.ResolveContract(value.GetType());

        foreach (var prop in propertyNames)
        {
          // Ignore properties starting with a double underscore.
          if (prop.StartsWith("__", StringComparison.CurrentCulture)) continue;

          var property = contract.Properties.GetClosestMatchProperty(prop);

          // Ignore properties decorated with [JsonIgnore].
          if (property != null && property.Ignored) continue;

          // Ignore nulls
          object propValue = obj[prop];
          if (propValue == null) continue;

          // Check if this property is marked for detachment.
          if (property != null)
          {
            var attrs = property.AttributeProvider.GetAttributes(typeof(DetachProperty), true);
            if (attrs.Count > 0)
              DetachLineage.Add(((DetachProperty)attrs[0]).Detachable);
            else
              DetachLineage.Add(false);
          }
          else if (prop.EndsWith("__")) // Convention check for dynamically added properties. Is it really needed?
            DetachLineage.Add(true);
          else
            DetachLineage.Add(false);

          // Set and store a reference, if it is marked as detachable and the transport is not null.
          if (propValue is Base && DetachLineage[DetachLineage.Count - 1])
          {
            var reference = new Reference() { referencedId = ((Base)propValue).hash };
            TrackReferenceInTree(reference.referencedId);
            jo.Add(prop, JToken.FromObject(reference));
            JToken.FromObject(propValue, serializer); // Trigger next
          }
          else
          {
            jo.Add(prop, JToken.FromObject(propValue, serializer)); // Default route
          }

          // Pop detach lineage
          DetachLineage.RemoveAt(DetachLineage.Count - 1);
        }

        if (ReferenceTracker.ContainsKey(Lineage[Lineage.Count - 1]))
          jo.Add("__tree", JToken.FromObject(ReferenceTracker[Lineage[Lineage.Count - 1]]));

        Parsed.Add(Lineage[Lineage.Count - 1]);

        jo.WriteTo(writer);

        if (DetachLineage.Count == 0 || DetachLineage[DetachLineage.Count - 1])
          ObjectBucket.Add(Lineage[Lineage.Count - 1], jo.ToString());

        // Pop lineage tracker
        Lineage.RemoveAt(Lineage.Count - 1);

        return;
      }

      var type = value.GetType();

      // List handling
      if (typeof(IEnumerable).IsAssignableFrom(type) && !typeof(IDictionary).IsAssignableFrom(type) && type != typeof(string))
      {
        JArray arr = new JArray();
        foreach (var arrValue in ((IEnumerable)value))
        {
          if (arrValue is Base && DetachLineage[DetachLineage.Count - 1])
          {
            var reference = new Reference() { referencedId = ((Base)arrValue).hash };
            TrackReferenceInTree(reference.referencedId);

            arr.Add(JToken.FromObject(reference));
            JToken.FromObject(arrValue, serializer); // Trigger next
          }
          else
            arr.Add(JToken.FromObject(arrValue, serializer)); // Default route
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
          JToken jToken;
          if (kvp.Value is Base && DetachLineage[DetachLineage.Count - 1])
          {
            var reference = new Reference() { referencedId = ((Base)kvp.Value).hash };
            TrackReferenceInTree(reference.referencedId);
            jToken = JToken.FromObject(reference);
            JToken.FromObject(kvp.Value, serializer); // Trigger next
          }
          else
          {
            jToken = JToken.FromObject(kvp.Value, serializer); // Default route
          }
          dictJo.Add(kvp.Key.ToString(), jToken);
        }
        dictJo.WriteTo(writer);
        return;
      }

      // Primitives, and all others
      var t = JToken.FromObject(value); // bypasses this converter
      t.WriteTo(writer);
    }

    #endregion

  }

}
