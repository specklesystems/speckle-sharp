using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Linq;
using Speckle.Newtonsoft.Json.Serialization;

namespace Speckle.Core.Serialisation
{
  /// <summary>
  /// Json converter that handles base speckle objects. Enables detachment &
  /// simultaneous transport (persistence) of objects. 
  /// </summary>
  public class BaseObjectSerializer : JsonConverter
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

    /// <summary>
    /// List of transports to write to.
    /// </summary>
    public List<ITransport> WriteTransports { get; set; } = new List<ITransport>();

    #region Write Json Helper Properties

    /// <summary>
    /// Keeps track of wether current property pointer is marked for detachment.
    /// </summary>
    List<bool> DetachLineage { get; set; }

    /// <summary>
    /// Keeps track of the hash chain through the object tree.
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

    public Action<string, Exception> OnErrorAction { get; set; }

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

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {

      if (CancellationToken.IsCancellationRequested)
      {
        return null; // Check for cancellation
      }

      if (reader.TokenType == JsonToken.Null)
      {
        return null;
      }

      // Check if we passed in an array, rather than an object.
      // TODO: Test the following branch. It's not used anywhere at the moment, and the default serializer prevents it from
      // ever being used (only allows single object serialization)
      if (reader.TokenType == JsonToken.StartArray)
      {
        var list = new List<Base>();
        var jarr = JArray.Load(reader);

        foreach (var val in jarr)
        {
          if (CancellationToken.IsCancellationRequested)
          {
            return null; // Check for cancellation
          }

          var whatever = SerializationUtilities.HandleValue(val, serializer, CancellationToken);
          list.Add(whatever as Base);
        }
        return list;
      }

      if (CancellationToken.IsCancellationRequested)
      {
        return null; // Check for cancellation
      }

      var jObject = JObject.Load(reader);

      if (jObject == null)
      {
        return null;
      }

      var objType = jObject.GetValue(TypeDiscriminator);

      // Assume dictionary!
      if (objType == null)
      {
        var dict = new Dictionary<string, object>();

        foreach (var val in jObject)
        {
          if (CancellationToken.IsCancellationRequested)
          {
            return null; // Check for cancellation
          }

          dict[val.Key] = SerializationUtilities.HandleValue(val.Value, serializer, CancellationToken);
        }
        return dict;
      }

      if (CancellationToken.IsCancellationRequested)
      {
        return null; // Check for cancellation
      }

      var discriminator = Extensions.Value<string>(objType);

      // Check for references.
      if (discriminator == "reference")
      {
        var id = Extensions.Value<string>(jObject.GetValue("referencedId"));
        string str = "";

        if (ReadTransport != null)
        {
          str = ReadTransport.GetObject(id);
        }
        else
        {
          throw new SpeckleException("Cannot resolve reference, no transport is defined.");
        }

        if (str != null && str != "")
        {
          jObject = JObject.Parse(str);
          discriminator = Extensions.Value<string>(jObject.GetValue(TypeDiscriminator));
        }
        else
        {
          throw new SpeckleException("Cannot resolve reference. The provided transport could not find it.");
        }
      }

      var type = SerializationUtilities.GetType(discriminator);
      var obj = existingValue ?? Activator.CreateInstance(type);

      var contract = (JsonDynamicContract)serializer.ContractResolver.ResolveContract(type);
      var used = new HashSet<string>();

      // remove unsettable properties
      jObject.Remove(TypeDiscriminator);
      jObject.Remove("__closure");

      if (CancellationToken.IsCancellationRequested)
      {
        return null; // Check for cancellation
      }

      foreach (var jProperty in jObject.Properties())
      {
        if (CancellationToken.IsCancellationRequested)
        {
          return null; // Check for cancellation
        }

        if (used.Contains(jProperty.Name))
        {
          continue;
        }

        used.Add(jProperty.Name);

        // first attempt to find a settable property, otherwise fall back to a dynamic set without type
        JsonProperty property = contract.Properties.GetClosestMatchProperty(jProperty.Name);

        if (property != null && property.Writable)
        {

          if (type == typeof(Abstract) && property.PropertyName == "base")
          {
            var propertyValue = SerializationUtilities.HandleAbstractOriginalValue(jProperty.Value, ((JValue)jObject.GetValue("assemblyQualifiedName")).Value as string, serializer);
            property.ValueProvider.SetValue(obj, propertyValue);
          }
          else
          {
            var val = SerializationUtilities.HandleValue(jProperty.Value, serializer, CancellationToken, property);
            property.ValueProvider.SetValue(obj, val);
          }
        }
        else
        {
          // dynamic properties
          CallSiteCache.SetValue(jProperty.Name, obj, SerializationUtilities.HandleValue(jProperty.Value, serializer, CancellationToken));
        }
      }

      if (CancellationToken.IsCancellationRequested)
      {
        return null; // Check for cancellation
      }

      TotalProcessedCount++;
      OnProgressAction?.Invoke("DS", 1);

      foreach (var callback in contract.OnDeserializedCallbacks)
      {
        callback(obj, serializer.Context);
      }

      return obj;
    }

    #endregion

    #region Write Json

    // Keeps track of the actual tree structure of the objects being serialised.
    // These tree references will thereafter be stored in the __tree prop. 
    private void TrackReferenceInTree(string refId)
    {
      // Help with creating closure table entries.
      for (int i = 0; i < Lineage.Count; i++)
      {
        var parent = Lineage[i];

        if (!RefMinDepthTracker.ContainsKey(parent))
        {
          RefMinDepthTracker[parent] = new Dictionary<string, int>();
        }

        if (!RefMinDepthTracker[parent].ContainsKey(refId))
        {
          RefMinDepthTracker[parent][refId] = Lineage.Count - i;
        }
        else if (RefMinDepthTracker[parent][refId] > Lineage.Count - i)
        {
          RefMinDepthTracker[parent][refId] = Lineage.Count - i;
        }
      }
    }

    private bool FirstEntry = true, FirstEntryWasListOrDict = false;

    // While this function looks complicated, it's actually quite smooth:
    // The important things to remember is that serialization goes depth first:
    // The first object to get fully serialised is the first nested one, with
    // the parent object being last. 
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      writer.Formatting = serializer.Formatting;
      if (CancellationToken.IsCancellationRequested)
      {
        return; // Check for cancellation
      }

      /////////////////////////////////////
      // Path one: nulls
      /////////////////////////////////////

      if (value == null)
      {
        return;
      }

      /////////////////////////////////////
      // Path two: primitives (string, bool, int, etc)
      /////////////////////////////////////

      if (value.GetType().IsPrimitive || value is string)
      {
        FirstEntry = false;
        writer.WriteValue(value);
        //var t = JToken.FromObject(value); // bypasses this converter as we do not pass in the serializer
        //t.WriteTo(writer);
        return;
      }

      /////////////////////////////////////
      // Path three: Bases
      /////////////////////////////////////

      if (value is Base && !(value is ObjectReference))
      {
        if (CancellationToken.IsCancellationRequested)
        {
          return; // Check for cancellation
        }

        var obj = value as Base;

        FirstEntry = false;
        //TotalProcessedCount++;

        // Append to lineage tracker
        Lineage.Add(Guid.NewGuid().ToString());

        var jo = new JObject();
        var propertyNames = obj.GetDynamicMemberNames();

        var contract = (JsonDynamicContract)serializer.ContractResolver.ResolveContract(value.GetType());

        // Iterate through the object's properties, one by one, checking for ignored ones
        foreach (var prop in propertyNames)
        {
          if (CancellationToken.IsCancellationRequested)
          {
            return; // Check for cancellation
          }
          // Ignore properties starting with a double underscore.
          if (prop.StartsWith("__"))
          {
            continue;
          }

          if (prop == "id")
          {
            continue;
          }

          var property = contract.Properties.GetClosestMatchProperty(prop);

          // Ignore properties decorated with [JsonIgnore].
          if (property != null && property.Ignored)
          {
            continue;
          }

          // Ignore nulls
          object propValue = obj[prop];
          if (propValue == null)
          {
            continue;
          }

          // Check if this property is marked for detachment: either by the presence of "@" at the beginning of the name, or by the presence of a DetachProperty attribute on a typed property.
          if (property != null)
          {
            var detachableAttributes = property.AttributeProvider.GetAttributes(typeof(DetachProperty), true);
            if (detachableAttributes.Count > 0)
            {
              DetachLineage.Add(((DetachProperty)detachableAttributes[0]).Detachable);
            }
            else
            {
              DetachLineage.Add(false);
            }

            var chunkableAttributes = property.AttributeProvider.GetAttributes(typeof(Chunkable), true);
            if (chunkableAttributes.Count > 0)
            {
              //DetachLineage.Add(true); // NOOPE
              serializer.Context = new StreamingContext(StreamingContextStates.Other, chunkableAttributes[0]);
            }
            else
            {
              //DetachLineage.Add(false);
              serializer.Context = new StreamingContext();
            }
          }
          else if (prop.StartsWith("@")) // Convention check for dynamically added properties.
          {
            DetachLineage.Add(true);

            var chunkSyntax = new Regex(@"^@\((\d*)\)");

            if (chunkSyntax.IsMatch(prop))
            {
              int chunkSize;
              var match = chunkSyntax.Match(prop);
              int.TryParse(match.Groups[match.Groups.Count - 1].Value, out chunkSize);
              serializer.Context = new StreamingContext(StreamingContextStates.Other,
                chunkSize > 0 ? new Chunkable(chunkSize) : new Chunkable());
            }
            else
            {
              serializer.Context = new StreamingContext();
            }
          }
          else
          {
            DetachLineage.Add(false);
          }

          // Set and store a reference, if it is marked as detachable and the transport is not null.
          if (WriteTransports != null && WriteTransports.Count != 0 && propValue is Base && DetachLineage[DetachLineage.Count - 1])
          {
            var what = JToken.FromObject(propValue, serializer); // Trigger next.

            if (CancellationToken.IsCancellationRequested)
            {
              return; // Check for cancellation
            }

            if (what == null)
            {
              return; // HACK: Prevent nulls from borking our serialization on nested schema object refs. (i.e. Line has @SchemaObject, that has ref to line)
            }

            var refHash = ((JObject)what).GetValue("id").ToString();

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
          // Refresh the streaming context to remove chunking flag
          serializer.Context = new StreamingContext();
        }

        // Check if we actually have any transports present that would warrant a 
        if ((WriteTransports != null && WriteTransports.Count != 0) && RefMinDepthTracker.ContainsKey(Lineage[Lineage.Count - 1]))
        {
          jo.Add("__closure", JToken.FromObject(RefMinDepthTracker[Lineage[Lineage.Count - 1]]));
        }

        var hash = Models.Utilities.hashString(jo.ToString());
        if (!jo.ContainsKey("id"))
        {
          jo.Add("id", JToken.FromObject(hash));
        }
        jo.WriteTo(writer);

        if ((DetachLineage.Count == 0 || DetachLineage[DetachLineage.Count - 1]) && WriteTransports != null && WriteTransports.Count != 0)
        {
          var objString = jo.ToString(writer.Formatting);
          var objId = jo["id"].Value<string>();

          OnProgressAction?.Invoke("S", 1);

          foreach (var transport in WriteTransports)
          {
            if (CancellationToken.IsCancellationRequested)
            {
              continue; // Check for cancellation
            }

            transport.SaveObject(objId, objString);
          }
        }

        // Pop lineage tracker
        Lineage.RemoveAt(Lineage.Count - 1);
        return;
      }

      /////////////////////////////////////
      // Path four: lists/arrays & dicts
      /////////////////////////////////////

      if (CancellationToken.IsCancellationRequested)
      {
        return; // Check for cancellation
      }

      var type = value.GetType();

      // TODO: List handling and dictionary serialisation handling can be sped up significantly if we first check by their inner type.
      // This handles a broader case in which we are, essentially, checking only for object[] or List<object> / Dictionary<string, object> cases.
      // A much faster approach is to check for List<primitive>, where primitive = string, number, etc. and directly serialize it in full.
      // Same goes for dictionaries.
      if (typeof(IEnumerable).IsAssignableFrom(type) && !typeof(IDictionary).IsAssignableFrom(type) && type != typeof(string))
      {

        if (TotalProcessedCount == 0 && FirstEntry)
        {
          FirstEntry = false;
          FirstEntryWasListOrDict = true;
          TotalProcessedCount += 1;
          DetachLineage.Add(WriteTransports != null && WriteTransports.Count != 0 ? true : false);
        }

        JArray arr = new JArray();

        // Chunking large lists into manageable parts.
        if (DetachLineage[DetachLineage.Count - 1] && serializer.Context.Context is Chunkable chunkInfo)
        {
          var maxCount = chunkInfo.MaxObjCountPerChunk;
          var i = 0;
          var chunkList = new List<DataChunk>();
          var currChunk = new DataChunk();

          foreach (var arrValue in ((IEnumerable)value))
          {
            if (i == maxCount)
            {
              if (currChunk.data.Count != 0)
              {
                chunkList.Add(currChunk);
              }

              currChunk = new DataChunk();
              i = 0;
            }
            currChunk.data.Add(arrValue);
            i++;
          }

          if (currChunk.data.Count != 0)
          {
            chunkList.Add(currChunk);
          }
          value = chunkList;

        }

        foreach (var arrValue in ((IEnumerable)value))
        {
          if (CancellationToken.IsCancellationRequested)
          {
            return; // Check for cancellation
          }

          if (arrValue == null)
          {
            continue;
          }

          if (WriteTransports != null && WriteTransports.Count != 0 && arrValue is Base && DetachLineage[DetachLineage.Count - 1])
          {
            var what = JToken.FromObject(arrValue, serializer); // Trigger next

            var refHash = ((JObject)what).GetValue("id").ToString();

            var reference = new ObjectReference() { referencedId = refHash };
            TrackReferenceInTree(refHash);
            arr.Add(JToken.FromObject(reference));
          }
          else
          {
            arr.Add(JToken.FromObject(arrValue, serializer)); // Default route
          }
        }

        if (CancellationToken.IsCancellationRequested)
        {
          return; // Check for cancellation
        }

        arr.WriteTo(writer);

        if (DetachLineage.Count == 1 && FirstEntryWasListOrDict) // are we in a list entry point case?
        {
          DetachLineage.RemoveAt(0);
        }

        return;
      }

      if (CancellationToken.IsCancellationRequested)
      {
        return; // Check for cancellation
      }

      if (typeof(IDictionary).IsAssignableFrom(type))
      {
        if (TotalProcessedCount == 0 && FirstEntry)
        {
          FirstEntry = false;
          FirstEntryWasListOrDict = true;
          TotalProcessedCount += 1;
          DetachLineage.Add(WriteTransports != null && WriteTransports.Count != 0 ? true : false);
        }
        var dict = value as IDictionary;
        var dictJo = new JObject();
        foreach (DictionaryEntry kvp in dict)
        {
          if (CancellationToken.IsCancellationRequested)
          {
            return; // Check for cancellation
          }

          if (kvp.Value == null)
          {
            continue;
          }

          JToken jToken;
          if (WriteTransports != null && WriteTransports.Count != 0 && kvp.Value is Base && DetachLineage[DetachLineage.Count - 1])
          {
            var what = JToken.FromObject(kvp.Value, serializer); // Trigger next
            var refHash = ((JObject)what).GetValue("id").ToString();

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

        if (CancellationToken.IsCancellationRequested)
        {
          return; // Check for cancellation
        }

        if (DetachLineage.Count == 1 && FirstEntryWasListOrDict) // are we in a dictionary entry point case?
        {
          DetachLineage.RemoveAt(0);
        }

        return;
      }

      /////////////////////////////////////
      // Path five: everything else (enums?)
      /////////////////////////////////////

      if (CancellationToken.IsCancellationRequested)
      {
        return; // Check for cancellation
      }

      FirstEntry = false;
      var lastCall = JToken.FromObject(value); // bypasses this converter as we do not pass in the serializer
      lastCall.WriteTo(writer);
    }

    #endregion

  }

}
