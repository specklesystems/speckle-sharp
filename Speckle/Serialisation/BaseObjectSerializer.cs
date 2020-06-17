using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Speckle.Models;
using Speckle.Transports;

namespace Speckle.Serialisation
{
  /// <summary>
  /// Json converter that handles base speckle objects. Enables detachment &
  /// simultaneous transport (persistance) of objects. 
  /// </summary>
  public class BaseObjectSerializer : Newtonsoft.Json.JsonConverter
  {

    /// <summary>
    /// Property that describes the type of the object.
    /// </summary>
    public string TypeDiscriminator = "speckle_type";

    /// <summary>
    /// Session transport keeps track, in this serialisation pass only, of what we've serialised.
    /// </summary>
    //public ITransport SessionTransport { get; set; }

    /// <summary>
    /// The sync transport. This transport will be used synchronously. 
    /// </summary>
    public ITransport Transport { get; set; }

    #region Write Json Helper Properties

    /// <summary>
    /// Keeps track of wether current property pointer is marked for detachment.
    /// </summary>
    List<bool> DetachLineage { get; set; }

    /// <summary>
    /// Keeps track of the hash chain throught the object tree.
    /// </summary>
    List<string> Lineage { get; set; }

    /// <summary>
    /// Dictionary of object if and its subsequent closure table (a dictionary of hashes and min depth at which they are found).
    /// </summary>
    Dictionary<string, Dictionary<string, int>> RefMinDepthTracker { get; set; }

    public int TotalProcessedCount = 0;
    #endregion

    public override bool CanWrite => true;

    public override bool CanRead => true;

    public Action<string, int> OnProgressAction { get; set; }

    public BaseObjectSerializer()
    {
      ResetAndInitialize();
    }

    /// <summary>
    /// Reinitializes the lineage, and other variables that get used during the
    /// json writing process.
    /// </summary>
    public void ResetAndInitialize()
    {
      DetachLineage = new List<bool>();
      Lineage = new List<string>();
      RefMinDepthTracker = new Dictionary<string, Dictionary<string, int>>();
      OnProgressAction = null;
      TotalProcessedCount = 0;
  }

    public override bool CanConvert(Type objectType) => true;

    #region Read Json

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
    {
      if (reader.TokenType == JsonToken.Null)
        return null;

      // Check if we passed in an array, rather than an object.
      // TODO: Test the following branch. It's not used anywhere at the moment, and the default serializer prevents it from
      // ever being used (only allows single object serialization)
      if (reader.TokenType == JsonToken.StartArray)
      {
        var list = new List<Base>();
        var jarr = JArray.Load(reader);

        foreach (var val in jarr)
        {
          var whatever = SerializationUtilities.HandleValue(val, serializer);
          list.Add(whatever as Base);
        }
        return list;
      }

      var jObject = JObject.Load(reader);

      if (jObject == null)
        return null;

      var discriminator = Extensions.Value<string>(jObject.GetValue(TypeDiscriminator));

      // Check for references.
      if (discriminator == "reference")
      {
        var id = Extensions.Value<string>(jObject.GetValue("referencedId"));
        string str;

        if (Transport != null)
          str = Transport.GetObject(id);
        else
          throw new Exception($"Cannot resolve reference with id of {id}: a transport is not defined.");

        if (str != null && str != "")
        {
          jObject = JObject.Parse(str);
          discriminator = Extensions.Value<string>(jObject.GetValue(TypeDiscriminator));
        }
        else
          throw new Exception($"Cannot resolve reference with id of {id}. The provided transport could not find it.");
      }

      var type = SerializationUtilities.GetType(discriminator);
      var obj = existingValue ?? Activator.CreateInstance(type);

      var contract = (JsonDynamicContract)serializer.ContractResolver.ResolveContract(type);
      var used = new HashSet<string>();

      // remove unsettable properties
      jObject.Remove(TypeDiscriminator);
      jObject.Remove("__closure");

      foreach (var jProperty in jObject.Properties())
      {
        if (used.Contains(jProperty.Name)) continue;

        used.Add(jProperty.Name);

        // first attempt to find a settable property, otherwise fall back to a dynamic set without type
        JsonProperty property = contract.Properties.GetClosestMatchProperty(jProperty.Name);

        if (property != null && property.Writable && !property.Ignored)
        {
          if (type == typeof(Abstract) && property.PropertyName == "base")
          {
            var propertyValue = SerializationUtilities.HandleAbstractOriginalValue(jProperty.Value, ((JValue)jObject.GetValue("assemblyQualifiedName")).Value as string, serializer);
            property.ValueProvider.SetValue(obj, propertyValue);
          }
          else
            property.ValueProvider.SetValue(obj, SerializationUtilities.HandleValue(jProperty.Value, serializer, property));
        }
        else
        {
          // dynamic properties
          CallSiteCache.SetValue(jProperty.Name, obj, SerializationUtilities.HandleValue(jProperty.Value, serializer));
        }
      }

      OnProgressAction?.Invoke(Transport.TransportName, ++TotalProcessedCount);
      return obj;
    }

    #endregion

    #region Write Json

    // Keeps track of the actual tree structure of the objects being serialised.
    // These tree references will thereafter be stored in the __tree prop. 
    void TrackReferenceInTree(string refId)
    {
      // Help with creating closure table entries.
      for (int i = 0; i < Lineage.Count; i++)
      {
        var parent = Lineage[i];

        if (!RefMinDepthTracker.ContainsKey(parent)) RefMinDepthTracker[parent] = new Dictionary<string, int>();

        if (!RefMinDepthTracker[parent].ContainsKey(refId)) RefMinDepthTracker[parent][refId] = Lineage.Count - i;
        else if (RefMinDepthTracker[parent][refId] > Lineage.Count - i) RefMinDepthTracker[parent][refId] = Lineage.Count - i;
      }
    }

    // While this function looks complicated, it's actually quite smooth:
    // The important things to remember is that serialization goes depth first:
    // The first object to get fully serialised is the first nested one, with
    // the parent object being last. 
    public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
    {
      if (value == null) return;

      if (value is Base && !(value is ObjectReference))
      {
        var obj = value as Base;
        //CurrentParentObjectHash = ;

        // Append to lineage tracker
        Lineage.Add( Guid.NewGuid().ToString());

        var jo = new JObject();
        var propertyNames = obj.GetDynamicMemberNames();

        var contract = (JsonDynamicContract)serializer.ContractResolver.ResolveContract(value.GetType());

        // Iterate through the object's properties, one by one, checking for ignored ones
        foreach (var prop in propertyNames)
        {
          // Ignore properties starting with a double underscore.
          if (prop.StartsWith("__")) continue;

          var property = contract.Properties.GetClosestMatchProperty(prop);

          // Ignore properties decorated with [JsonIgnore].
          if (property != null && property.Ignored) continue;

          // Ignore nulls
          object propValue = obj[prop];
          if (propValue == null) continue;

          // Check if this property is marked for detachment: either by the presence of "@" at the beginning of the name, or by the presence of a DetachProperty attribute on a typed property.
          if (property != null)
          {
            var attrs = property.AttributeProvider.GetAttributes(typeof(DetachProperty), true);
            if (attrs.Count > 0)
            {
              DetachLineage.Add(((DetachProperty)attrs[0]).Detachable);
            }
            else
            {
              DetachLineage.Add(false);
            }
          }
          else if (prop.StartsWith("@")) // Convention check for dynamically added properties.
            DetachLineage.Add(true);
          else
            DetachLineage.Add(false);

          // Set and store a reference, if it is marked as detachable and the transport is not null.
          if (Transport != null && propValue is Base && DetachLineage[DetachLineage.Count - 1])
          {
            var what = JToken.FromObject(propValue, serializer); // Trigger next.
            var refHash = ((JObject)what).GetValue("hash").ToString();

            var reference = new ObjectReference() { referencedId = refHash };
            TrackReferenceInTree(refHash);
            jo.Add(prop, JToken.FromObject(reference));
          }
          else
          {
            jo.Add(prop, JToken.FromObject(propValue, serializer)); // Default route
          }

          // Pop detach lineage. If you don't get this, remember this thing moves ONLY FORWARD, DEPTH FIRST
          DetachLineage.RemoveAt(DetachLineage.Count - 1);
        }

        // Check if we actually have any transports present that would warrant a 
        if ((Transport != null) && RefMinDepthTracker.ContainsKey(Lineage[Lineage.Count - 1]))
        {
          jo.Add("__closure", JToken.FromObject(RefMinDepthTracker[Lineage[Lineage.Count - 1]]));
        }

        var hash = Models.Utilities.hashString(jo.ToString());
        jo.Add("hash", JToken.FromObject(hash));
        jo.WriteTo(writer);

        if ((DetachLineage.Count == 0 || DetachLineage[DetachLineage.Count - 1]) && (Transport != null))
        {
          var objString = jo.ToString();
          var objId = jo["hash"].Value<string>();

          Transport.SaveObject(objId, objString);

          OnProgressAction?.Invoke(Transport.TransportName, ++TotalProcessedCount);
        }

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
          if (Transport != null && arrValue is Base && DetachLineage[DetachLineage.Count - 1])
          {
            var what = JToken.FromObject(arrValue, serializer); // Trigger next
            var refHash = ((JObject)what).GetValue("hash").ToString();

            var reference = new ObjectReference() { referencedId = refHash };
            TrackReferenceInTree(refHash);
            arr.Add(JToken.FromObject(reference));
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
          if (Transport != null && kvp.Value is Base && DetachLineage[DetachLineage.Count - 1])
          {
            var what = JToken.FromObject(kvp.Value, serializer); // Trigger next
            var refHash = ((JObject)what).GetValue("hash").ToString();

            var reference = new ObjectReference() { referencedId = refHash };
            TrackReferenceInTree(refHash);
            jToken = JToken.FromObject(reference);
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
      var t = JToken.FromObject(value); // bypasses this converter as we do not pass in the serializer
      t.WriteTo(writer);
    }

    #endregion

  }

}
