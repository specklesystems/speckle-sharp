using System.Collections.Generic;
using DUI3.Bindings;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Serialization;

namespace DUI3.Utils;

public static class SerializationSettingsFactory
{
  /// <summary>
  /// Get the canonical Newtonsoft serialization/deserialization settings which we use in DUI3, which currently consist of a camel case name strategy and a discriminated object converter.
  /// </summary>
  /// <returns></returns>
  public static JsonSerializerSettings GetSerializerSettings()
  {
    var serializerOptions = new JsonSerializerSettings 
    {
      ContractResolver = new CamelCasePropertyNamesContractResolver(),
      NullValueHandling = NullValueHandling.Ignore,
      ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
      Converters =
      {
        new DiscriminatedObjectConverter(),
        new AbstractConverter<DiscriminatedObject, ISendFilter>()
      }
    };

    return serializerOptions;
  }
}
