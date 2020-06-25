using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Speckle.Models;
using Speckle.Serialisation;
using Speckle.Transports;

namespace Speckle.Core
{
  /// <summary>
  /// Exposes several key methods for interacting with Speckle.
  /// <para>Serialize/Deserialize</para>
  /// <para>Push/Pull (methods to serialize & send data to one or more servers)</para>
  /// </summary>
  public static partial class Operations
  {
    /// <summary>
    /// Instantiates an instance of the default object serializer and settings pre-populated with it. 
    /// <returns>A tuple of Serializer and Settings.</returns>
    public static (BaseObjectSerializer, JsonSerializerSettings) GetSerializerInstance()
    {
      var serializer = new BaseObjectSerializer();
      var settings = new JsonSerializerSettings()
      {
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Formatting = Formatting.None,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        Converters = new List<Newtonsoft.Json.JsonConverter> { serializer }
      };

      return (serializer, settings);
    }



    #region Getting objects
    // TODO
    #endregion
  }

}
