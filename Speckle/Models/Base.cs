using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Speckle.Models
{

  /// <summary>
  /// Base class for all Speckle object definitions. Provides unified hashing, type extraction and serialisation.
  /// </summary>
  [Serializable]
  public class Base : DynamicBase
  {
    /// <summary>
    /// Overrdiable hash. Ideally all objects would implement an efficient method 
    /// </summary>
    public virtual string hash
    {
      get
      {
        var str = JsonConvert.SerializeObject(this, new JsonSerializerSettings()
        {
          NullValueHandling = NullValueHandling.Ignore,
          ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
          Converters = new List<JsonConverter> { new HashSerialiser() },
        });
        return Utilities.hashString(str);
      }
    }

    public string applicationId { get; set; }

    private string __type;

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
