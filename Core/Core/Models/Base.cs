using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Serialisation;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Linq;

namespace Speckle.Core.Models;

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
  private static readonly Regex ChunkSyntax = new(@"^@\((\d*)\)"); //TODO: this same regex is duplicated a few times across the code base, we could consolidate them

  private string __type;

  /// <summary>
  /// A speckle object's id is an unique hash based on its properties. <b>NOTE: this field will be null unless the object was deserialised from a source. Use the <see cref="GetId(bool)"/> function to get it.</b>
  /// </summary>
  [SchemaIgnore]
  public virtual string id { get; set; }

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
        List<string> bases = new();
        Type myType = GetType();

        while (myType.Name != nameof(Base))
        {
          if (!myType.IsAbstract)
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
        s.WriteTransports = new List<ITransport> { new MemoryTransport() };
      var obj = JsonConvert.SerializeObject(this, t);
      return JObject.Parse(obj).GetValue(nameof(id)).ToString();
    }
    else
    {
      var s = new BaseObjectSerializerV2();
      if (decompose)
        s.WriteTransports = new List<ITransport> { new MemoryTransport() };
      var obj = s.Serialize(this);
      return JObject.Parse(obj).GetValue(nameof(id)).ToString();
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

  private static long CountDescendants(Base @base, HashSet<int> parsed)
  {
    if (parsed.Contains(@base.GetHashCode()))
      return 0;

    parsed.Add(@base.GetHashCode());

    long count = 0;
    var typedProps = @base.GetInstanceMembers();
    foreach (var prop in typedProps.Where(p => p.CanRead))
    {
      bool isIgnored =
        prop.IsDefined(typeof(ObsoleteAttribute), true) || prop.IsDefined(typeof(JsonIgnoreAttribute), true);
      if (isIgnored)
        continue;

      var detachAttribute = prop.GetCustomAttribute<DetachProperty>(true);
      var chunkAttribute = prop.GetCustomAttribute<Chunkable>(true);

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
          count += asList.Count / chunkAttribute.MaxObjCountPerChunk;
      }
    }

    var dynamicProps = @base.GetDynamicMembers();
    foreach (var propName in dynamicProps)
    {
      if (!propName.StartsWith("@"))
        continue;

      // Simplfied dynamic prop chunking handling
      if (ChunkSyntax.IsMatch(propName))
      {
        int chunkSize = -1;
        var match = ChunkSyntax.Match(propName);
        int.TryParse(match.Groups[match.Groups.Count - 1].Value, out chunkSize);

        if (chunkSize != -1 && @base[propName] is IList asList)
        {
          count += asList.Count / chunkSize;
          continue;
        }
      }

      count += HandleObjectCount(@base[propName], parsed);
    }

    return count;
  }

  private static long HandleObjectCount(object value, HashSet<int> parsed)
  {
    long count = 0;
    switch (value)
    {
      case Base b:
        count++;
        count += CountDescendants(b, parsed);
        return count;
      case IDictionary d:
      {
        foreach (DictionaryEntry kvp in d)
          if (kvp.Value is Base)
          {
            count++;
            count += CountDescendants(kvp.Value as Base, parsed);
          }
          else
          {
            count += HandleObjectCount(kvp.Value, parsed);
          }

        return count;
      }
      case IEnumerable e when !(value is string):
      {
        foreach (var arrValue in e)
          if (arrValue is Base)
          {
            count++;
            count += CountDescendants(arrValue as Base, parsed);
          }
          else
          {
            count += HandleObjectCount(arrValue, parsed);
          }

        return count;
      }
      default:
        return count;
    }
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

    foreach (
      var kvp in GetMembers(
        DynamicBaseMemberType.Instance | DynamicBaseMemberType.Dynamic | DynamicBaseMemberType.SchemaIgnored
      )
    )
    {
      var p = GetType().GetProperty(kvp.Key);
      if (p != null && !p.CanWrite)
        continue;

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
}

public class Blob : Base
{
  [JsonIgnore]
  public static int LocalHashPrefixLength = 20;

  private string _filePath;
  private string _hash;
  private bool hashExpired = true;

  public Blob() { }

  public Blob(string filePath)
  {
    this.filePath = filePath;
  }

  public string filePath
  {
    get => _filePath;
    set
    {
      if (originalPath is null)
        originalPath = value;
      _filePath = value;
      hashExpired = true;
    }
  }

  public string originalPath { get; set; }

  /// <summary>
  /// For blobs, the id is the same as the file hash. Please note, when deserialising, the id will be set from the original hash generated on sending.
  /// </summary>
  public override string id
  {
    get => GetFileHash();
    set => base.id = value;
  }

  public string GetFileHash()
  {
    if ((hashExpired || _hash == null) && filePath != null)
      _hash = Utilities.hashFile(filePath);

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
}
