using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Linq;

namespace Speckle.Core.Models
{

  /// <summary>
  /// Base class for all Speckle object definitions. Provides unified hashing, type extraction and serialisation.
  /// <para>When developing a speckle kit, use this class as a parent class.</para>
  /// <para><b>Dynamic properties naming conventions:</b></para>
  /// <para>👉 "__" at the start of a property means it will be ignored, both for hashing and serialisation (e.g., "__ignoreMe").</para>
  /// <para>👉 "@" at the start of a property name means it will be detached (when serialised with a transport) (e.g.((dynamic)obj)["@meshEquivalent"] = ...) .</para>
  /// </summary>
  [Serializable]
  public class Base : DynamicBase
  {
    /// <summary>
    /// A speckle object's id is an unique hash based on its properties. <b>NOTE: this field will be null unless the object was deserialised from a source. Use the <see cref="GetId(bool)"/> function to get it.</b>
    /// </summary>
    [SchemaIgnore]
    public virtual string id
    {
      get; set;
    }

    /// <summary>
    /// Gets the id (a unique hash) of this object. ⚠️ This method fully serializes the object, which in the case of large objects (with many sub-objects), has a tangible cost. Avoid using it!
    /// <para><b>Hint:</b> Objects that are retrieved/pulled from a server/local cache do have an id (hash) property pre-populated.</para>
    /// <para><b>Note:</b>The hash of a decomposed object differs from the hash of a non-decomposed object.</para>
    /// </summary>
    /// <param name="decompose">If true, will decompose the object in the process of hashing.</param>
    /// <returns></returns>
    public string GetId(bool decompose = false, SerializerVersion serializerVersion = SerializerVersion.V2)
    {
      if (serializerVersion == SerializerVersion.V1)
      {
        var (s, t) = Operations.GetSerializerInstance();
        if (decompose)
        {
          s.WriteTransports = new List<ITransport>() { new MemoryTransport() };
        }
        var obj = JsonConvert.SerializeObject(this, t);
        return JObject.Parse(obj).GetValue("id").ToString();
      }
      else
      {
        var s = new Serialisation.BaseObjectSerializerV2();
        if (decompose)
        {
          s.WriteTransports = new List<ITransport>() { new MemoryTransport() };
        }
        var obj = s.Serialize(this);
        return JObject.Parse(obj).GetValue("id").ToString();
      }
    }

    /// <summary>
    /// Attempts to count the total number of detachable objects.
    /// </summary>
    /// <returns>The total count of the detachable children + 1 (itself).</returns>
    public long GetTotalChildrenCount()
    {
      var parsed = new HashSet<int>();
      return 1 + CountDescendants(this, parsed);
    }

    private long CountDescendants(Base @base, HashSet<int> parsed)
    {
      if (parsed.Contains(@base.GetHashCode()))
      {
        return 0;
      }

      parsed.Add(@base.GetHashCode());

      long count = 0;
      var typedProps = @base.GetInstanceMembers();
      foreach (var prop in typedProps.Where(p => p.CanRead))
      {
        var detachAttribute = prop.GetCustomAttribute<DetachProperty>(true);
        var chunkAttribute = prop.GetCustomAttribute<Chunkable>(true);
        var obsoleteAttr = prop.GetCustomAttribute<ObsoleteAttribute>(true);
        var jsonIgnoredAttr = prop.GetCustomAttribute<JsonIgnoreAttribute>(true);

        if (obsoleteAttr != null || jsonIgnoredAttr != null)
        {
          // Skip properties from the count that are:
          // - Obsolete
          // - Ignored by the serializer
          continue;
        }

        object value = prop.GetValue(@base);

        if (detachAttribute != null && detachAttribute.Detachable && chunkAttribute == null)
        {
          count += HandleObjectCount(value, parsed);
        }
        else if (detachAttribute != null && detachAttribute.Detachable && chunkAttribute != null)
        {
          // Simplified chunking count handling.
          var asList = value as IList;
          if (asList != null)
          {
            count += asList.Count / chunkAttribute.MaxObjCountPerChunk;
            continue;
          }
          var asArray = value as Array;
          if (asArray != null)
          {
            count += asArray.Length / chunkAttribute.MaxObjCountPerChunk;
            continue;
          }
        }
      }

      var dynamicProps = @base.GetDynamicMembers();
      var chunkSyntax = new System.Text.RegularExpressions.Regex(@"^@\((\d*)\)");
      foreach (var propName in dynamicProps)
      {
        if (!propName.StartsWith("@"))
        {
          continue;
        }

        // Simplfied dynamic prop chunking handling
        if (chunkSyntax.IsMatch(propName))
        {
          int chunkSize = -1;
          var match = chunkSyntax.Match(propName);
          int.TryParse(match.Groups[match.Groups.Count - 1].Value, out chunkSize);

          var asList = @base[propName] as IList;
          if (chunkSize != -1 && asList != null)
          {
            count += asList.Count / chunkSize;
            continue;
          }

          var asArr = @base[propName] as Array;
          if (chunkSize != -1 && asArr != null)
          {
            count += asArr.Length / chunkSize;
            continue;
          }
        }

        count += HandleObjectCount(@base[propName], parsed);
      }

      return count;
    }

    private long HandleObjectCount(object value, HashSet<int> parsed)
    {
      long count = 0;
      if (value == null)
      {
        return count;
      }

      if (value is Base)
      {
        count++;
        count += CountDescendants(value as Base, parsed);
        return count;
      }

      var propType = value.GetType();
      if (typeof(IEnumerable).IsAssignableFrom(propType) && !typeof(IDictionary).IsAssignableFrom(propType) && propType != typeof(string))
      {
        foreach (var arrValue in ((IEnumerable)value))
        {
          if (arrValue is Base)
          {
            count++;
            count += CountDescendants(arrValue as Base, parsed);
          }
          else
          {
            count += HandleObjectCount(arrValue, parsed);
          }
        }

        return count;
      }

      if (typeof(IDictionary).IsAssignableFrom(propType))
      {
        foreach (DictionaryEntry kvp in (IDictionary)value)
        {
          if (kvp.Value is Base)
          {
            count++;
            count += CountDescendants(kvp.Value as Base, parsed);
          }
          else
          {
            count += HandleObjectCount(kvp.Value, parsed);
          }
        }
        return count;
      }

      return count;
    }

    /// <summary>
    /// Creates a shallow copy of the current base object.
    /// This operation does NOT copy/duplicate the data inside each prop.
    /// The new object's property values will be pointers to the original object's property value.
    /// </summary>
    /// <returns>A shallow copy of the original object.</returns>
    public Base ShallowCopy()
    {
      var myDuplicate = (Base)Activator.CreateInstance(GetType());
      myDuplicate.id = id;
      myDuplicate.applicationId = applicationId;

      foreach (var kvp in GetMembers(
                 DynamicBaseMemberType.Instance
                 | DynamicBaseMemberType.Dynamic
                 | DynamicBaseMemberType.SchemaIgnored)
               )
      {
        var p = GetType().GetProperty(kvp.Key);
        if (p != null && !p.CanWrite)
        {
          continue;
        }

        try
        {
          myDuplicate[kvp.Key] = kvp.Value;
        }
        catch
        {
          // avoids any last ditch unsettable or strange props.
        }
      }

      return myDuplicate;
    }

    /// <summary>
    /// This property will only be populated if the object is retreieved from storage. Use <see cref="GetTotalChildrenCount"/> otherwise. 
    /// </summary>
    [SchemaIgnore]
    public virtual long totalChildrenCount { get; set; }

    /// <summary>
    /// Secondary, ideally host application driven, object identifier.
    /// </summary>
    [SchemaIgnore]
    public string applicationId { get; set; }


    private string __type;

    /// <summary>
    /// Holds the type information of this speckle object, derived automatically
    /// from its assembly name and inheritance.
    /// </summary>
    [SchemaIgnore]
    public virtual string speckle_type
    {
      get
      {
        if (__type == null)
        {
          List<string> bases = new List<string>();
          Type myType = this.GetType();

          while (myType.Name != nameof(Base))
          {
            bases.Add(myType.FullName);
            myType = myType.BaseType;
          }

          if (bases.Count == 0)
          {
            __type = nameof(Base);
          }
          else
          {
            bases.Reverse();
            __type = string.Join(":", bases);
          }
        }
        return __type;
      }
    }
  }


  public class Blob : Base
  {
    private string _hash;
    private bool hashExpired = true;

    private string _filePath;
    public string filePath
    {
      get => _filePath;
      set
      {
        if (originalPath is null) originalPath = value;
        _filePath = value;
        hashExpired = true;
      }
    }
    public string originalPath { get; set; }

    public Blob() { }

    public Blob(string filePath)
    {
      this.filePath = filePath;
    }

    /// <summary>
    /// For blobs, the id is the same as the file hash. Please note, when deserialising, the id will be set from the original hash generated on sending.
    /// </summary>
    public override string id { get => GetFileHash(); set => base.id = value; }

    public string GetFileHash()
    {
      if ((hashExpired || _hash == null) && filePath != null)
      {
        _hash = Utilities.hashFile(filePath);
      }

      return _hash;
    }

    [OnDeserialized]
    internal void OnDeserialized(StreamingContext context)
    {
      hashExpired = false;
    }

    public string getLocalDestinationPath(string blobStorageFolder)
    {
      var fileName = Path.GetFileName(filePath);
      return Path.Combine(blobStorageFolder, $"{id.Substring(0, 10)}-{fileName}");
    }

    [JsonIgnore]
    public static int LocalHashPrefixLength = 20;
  }
}
