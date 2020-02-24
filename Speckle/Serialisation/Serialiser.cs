using System;
using System.Collections.Generic;
using Speckle.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Collections;

namespace Speckle.Serialisation
{
  public class JsonConverter
  {
    public BaseObjectSerializer Converter;

    public JsonSerializerSettings ConversionSettings;

    public JsonConverter()
    {
      Converter = new BaseObjectSerializer();
      ConversionSettings = new JsonSerializerSettings()
      {
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
#if DEBUG
        Formatting = Formatting.Indented,
#else
      Formatting = Fromatting.None,
#endif
        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
        Converters = new List<Newtonsoft.Json.JsonConverter> { Converter }
      };
    }

    public Dictionary<string, string> Serialize(Base @base)
    {
      Converter.ObjectBucket = new Dictionary<string, string>();

      JsonConvert.SerializeObject(@base, ConversionSettings);

      //Converter.ObjectBucket.Reverse();

      return Converter.ObjectBucket;
    }

    public IEnumerable<Base> Deserialize(IEnumerable<string> @object)
    {
      return null;
    }
  }

}
