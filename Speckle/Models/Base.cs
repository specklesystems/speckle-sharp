using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Speckle.Models
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
    /// Unique hash based on the object's properties.
    /// <para>
    /// Override the hash property if you need/want to define a more efficient way to
    /// calculate it rather than the default "serialize" everything.
    /// </para>
    /// </summary>
    public virtual string id
    {
      get; set;
    }

    /// <summary>
    /// Intransient identifier that does not change when properties change. 
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
