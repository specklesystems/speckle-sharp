using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Serialization;

namespace DUI3.Utils;

public static class Serialization
{
  /// <summary>
  /// Get the canonical Newtonsoft serialization/deserialization settings which we use in DUI3.
  /// </summary>
  /// <returns></returns>
  public static JsonSerializerSettings GetSerializerSettings()
  {
    var serializerOptions = new JsonSerializerSettings 
    {
      ContractResolver = new CamelCasePropertyNamesContractResolver()
    };
      
    serializerOptions.Converters.Add(new DiscriminatedObjectConverter());

    return serializerOptions;
  }
}
