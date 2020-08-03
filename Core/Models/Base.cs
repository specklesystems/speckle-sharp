using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Speckle.Core.Api;
using Speckle.Core.Transports;

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
    /// A speckle object's id is an unique hash based on its properties.
    /// </summary>
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
    public string GetId(bool decompose = false)
    {
      var (s, t) = Operations.GetSerializerInstance();
      if(decompose)
      {
        s.Transport = new MemoryTransport();
      }
      var obj = JsonConvert.SerializeObject(this, t);
      return JObject.Parse(obj).GetValue("id").ToString();
    }

    /// <summary>
    /// Secondary, ideally host application driven, object identifier.
    /// </summary>
    public string applicationId { get; set; }

    private string __type;

    /// <summary>
    /// Holds the type information of this speckle object, derived automatically
    /// from its assembly name and inheritance.
    /// TODO: add versioning capabilities.
    /// </summary>
    public string speckle_type
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
          bases.Reverse();
          __type = string.Join(":", bases);
        }
        return __type;
      }
    }
  }
}
