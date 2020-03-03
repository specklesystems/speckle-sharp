using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Speckle.Kits;
using Speckle.Models;
using Speckle.Transports;

namespace Speckle.Serialisation
{
  public class BaseObjectSerializer : Newtonsoft.Json.JsonConverter
  {

    /// <summary>
    /// Session transport keeps track, in this serialisation pass only, of what we've serialised.
    /// </summary>
    public ITransport SessionTransport { get; set; }

    /// <summary>
    /// The Transport should actually 
    /// </summary>
    public ITransport Transport { get; set; }

    #region Write Json Helpers

    /// <summary>
    /// Keeps track of wether current property pointer is marked for detachment.
    /// </summary>
    List<bool> DetachLineage { get; set; }

    /// <summary>
    /// Keeps track of the hash chain throught the object tree.
    /// </summary>
    List<string> Lineage { get; set;}

    /// <summary>
    /// Tracks composed tree references for each object, as they get serialized.
    /// </summary>
    Dictionary<string, HashSet<string>> ReferenceTracker { get; set; }

    /// <summary>
    /// Holds all the parsed hashes within this session.
    /// </summary>
    HashSet<string> Parsed { get; set; }

    string CurrentParentObjectHash { get; set; }

    #endregion

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
      ReferenceTracker = new Dictionary<string, HashSet<string>>();
      Parsed = new HashSet<string>();
      CurrentParentObjectHash = "";
    }

    public override bool CanConvert(Type objectType) => true;

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
    {
      // TODO: implement
      throw new NotImplementedException();
    }

    #region Write Json

    // Keeps track of the actual tree structure of the objects being serialised.
    // These tree references will thereafter be stored in the __tree prop. 
    void TrackReferenceInTree(string refId)
    {
      var path = "";
      // Go backwards, to get the last one in first, so we can accumulate the
      // tree hashes chain.
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

    // While this function looks complicated, it's actually quite smooth:
    // The important things to remember is that serialization goes depth first:
    // The first object to get fully serialised is the first nested one, with
    // the parent object being last. 
    public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
    {
      if (value == null) return;

      if (value is Base && !(value is Reference))
      {
        var obj = value as Base;
        CurrentParentObjectHash = obj.hash;

        if (Parsed.Contains(CurrentParentObjectHash))
        {
          //var reference = new Reference() { referencedId = CurrentParentObjectHash };
          //TrackReferenceInTree(reference.referencedId);
          //jo.Add(prop, JToken.FromObject(reference));
          return;
        }

        // Append to lineage tracker
        Lineage.Add(CurrentParentObjectHash);

        var jo = new JObject();
        var propertyNames = obj.GetDynamicMemberNames();

        var contract = (JsonDynamicContract)serializer.ContractResolver.ResolveContract(value.GetType());

        // Iterate through the object's properties, one by one, checking for ignored ones
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
            JToken.FromObject(propValue, serializer); // Trigger next. 
          }
          else
          {
            jo.Add(prop, JToken.FromObject(propValue, serializer)); // Default route
          }

          // Pop detach lineage. If you don't get this, remember this thing moves ONLY FORWARD, DEPTH FIRST
          DetachLineage.RemoveAt(DetachLineage.Count - 1);
        }

        if (ReferenceTracker.ContainsKey(Lineage[Lineage.Count - 1]))
          jo.Add("__tree", JToken.FromObject(ReferenceTracker[Lineage[Lineage.Count - 1]]));

        Parsed.Add(Lineage[Lineage.Count - 1]);

        jo.WriteTo(writer);

        if (DetachLineage.Count == 0 || DetachLineage[DetachLineage.Count - 1])
        {
          var stringifiedObject = jo.ToString();

          // Stores/saves the object in the provided transports.
          // If an actual Transport is provided (besides the session transport),
          // the memory transport will just store the hash of the object; otherwise,
          // it will store the full serialized object. 

          SessionTransport.SaveObject(Lineage[Lineage.Count - 1], Transport == null ? stringifiedObject : Lineage[Lineage.Count - 1]);
          Transport?.SaveObject(Lineage[Lineage.Count - 1], stringifiedObject);
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
      var t = JToken.FromObject(value); // bypasses this converter as we do not pass in the serializer
      t.WriteTo(writer);
    }

    #endregion

  }

  internal static class SerializationUtilities
  {
    internal static class SharedUtilities
    {

      #region Getting Types

      private static Dictionary<string, Type> cachedTypes = new Dictionary<string, Type>();

      internal static Type GetType(string objFullType)
      {
        var objectTypes = objFullType.Split(':').Reverse();

        if (cachedTypes.ContainsKey(objectTypes.First()))
          return cachedTypes[objectTypes.First()];

        foreach (var typeName in objectTypes)
        {
          var type = KitManager.Types.FirstOrDefault(tp => tp.FullName == typeName);
          if (type != null)
          {
            cachedTypes[typeName] = type;
            return type;
          }
        }

        return typeof(Base);
      }

      #endregion

      #region value handling

      internal static object HandleValue(JToken value, Newtonsoft.Json.JsonSerializer serializer, JsonProperty jsonProperty = null, string TypeDiscriminator = "_type")
      {
        if (value is JValue)
        {
          return ((JValue)value).Value;
        }

        if (value is JArray)
        {
          if (jsonProperty != null && jsonProperty.PropertyType.GetConstructor(Type.EmptyTypes) != null)
          {
            var arr = jsonProperty != null ? Activator.CreateInstance(jsonProperty.PropertyType) : new List<object>();
            foreach (var val in ((JArray)value))
            {
              ((IList)arr).Add(HandleValue(val, serializer));
            }
            return arr;
          }
          else if (jsonProperty != null)
          {
            var arr = Activator.CreateInstance(typeof(List<>).MakeGenericType(jsonProperty.PropertyType.GetElementType()));
            var actualArr = Array.CreateInstance(jsonProperty.PropertyType.GetElementType(), ((JArray)value).Count);

            foreach (var val in ((JArray)value))
            {
              ((IList)arr).Add(Convert.ChangeType(HandleValue(val, serializer), jsonProperty.PropertyType.GetElementType()));
            }

            ((IList)arr).CopyTo(actualArr, 0);
            return actualArr;
          }
          else
          {
            var arr = new List<object>();
            foreach (var val in ((JArray)value))
            {
              arr.Add(HandleValue(val, serializer));
            }
            return arr;
          }
        }

        if (value is JObject)
        {
          if (((JObject)value).Property(TypeDiscriminator) != null)
          {
            return value.ToObject<Base>(serializer);
          }

          var dict = jsonProperty != null ? Activator.CreateInstance(jsonProperty.PropertyType) : new Dictionary<string, object>();
          foreach (var prop in ((JObject)value))
          {
            object key = prop.Key;
            if (jsonProperty != null)
              key = Convert.ChangeType(prop.Key, jsonProperty.PropertyType.GetGenericArguments()[0]);
            ((IDictionary)dict)[key] = HandleValue(prop.Value, serializer);
          }
          return dict;
        }
        return null;
      }

      #endregion

      #region Abstract Handling

      private static Dictionary<string, Type> cachedAbstractTypes = new Dictionary<string, Type>();

      internal static object HandleAbstractOriginalValue(JToken jToken, string assemblyQualifiedName, Newtonsoft.Json.JsonSerializer serializer)
      {
        if (cachedAbstractTypes.ContainsKey(assemblyQualifiedName))
          return jToken.ToObject(cachedAbstractTypes[assemblyQualifiedName]);

        var pieces = assemblyQualifiedName.Split(',').Select(s => s.Trim()).ToArray();

        var myAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(ass => ass.GetName().Name == pieces[1]);
        if (myAssembly == null) throw new Exception("Could not load abstract object's assembly.");

        var myType = myAssembly.GetType(pieces[0]);
        if (myType == null) throw new Exception("Could not load abstract object's assembly.");

        cachedAbstractTypes[assemblyQualifiedName] = myType;

        return jToken.ToObject(myType);
      }

      #endregion
    }

    internal static class CallSiteCache
    {
      // Adapted from the answer to 
      // https://stackoverflow.com/questions/12057516/c-sharp-dynamicobject-dynamic-properties
      // by jbtule, https://stackoverflow.com/users/637783/jbtule
      // And also
      // https://github.com/mgravell/fast-member/blob/master/FastMember/CallSiteCache.cs
      // by Marc Gravell, https://github.com/mgravell

      private static readonly Dictionary<string, CallSite<Func<CallSite, object, object, object>>> setters
        = new Dictionary<string, CallSite<Func<CallSite, object, object, object>>>();

      public static void SetValue(string propertyName, object target, object value)
      {
        CallSite<Func<CallSite, object, object, object>> site;

        lock (setters)
        {
          if (!setters.TryGetValue(propertyName, out site))
          {
            var binder = Microsoft.CSharp.RuntimeBinder.Binder.SetMember(CSharpBinderFlags.None,
                 propertyName, typeof(CallSiteCache),
                 new List<CSharpArgumentInfo>{
                   CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                   CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)});
            setters[propertyName] = site = CallSite<Func<CallSite, object, object, object>>.Create(binder);
          }
        }

        site.Target(site, target, value);
      }
    }
  }

}
