using System;
using System.Collections.Generic;
using Speckle.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Collections;
using Speckle.Transports;

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
        //Formatting = Formatting.Indented,
#else
        Formatting = Formatting.None,
#endif
        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
        Converters = new List<Newtonsoft.Json.JsonConverter> { Converter }
      };
    }

    /// <summary>
    /// Fully serializes an object, and returns its string representation.
    /// </summary>
    /// <param name="base"></param>
    /// <returns></returns>
    public string Serialize(Base @base)
    {
      Converter.ResetAndInitialize();
      return JsonConvert.SerializeObject(@base, ConversionSettings);
    }
    
    /// <summary>
    /// Serializes an object, and persists its constituent parts via the provided transport.
    /// </summary>
    /// <param name="base"></param>
    /// <param name="transport"></param>
    /// <returns></returns>
    public string SerializeAndSave(Base @base, ITransport transport)
    {
      Converter.ResetAndInitialize();
      Converter.Transport = transport;

      return JsonConvert.SerializeObject(@base, ConversionSettings);
    }

    /// <summary>
    /// Deserializes a fully serialized object. If any references are present, it will fail.
    /// </summary>
    /// <param name="object"></param>
    /// <returns></returns>
    public Base Deserialize(string @object)
    {
      Converter.ResetAndInitialize();
      return JsonConvert.DeserializeObject<Base>(@object, ConversionSettings);
    }

    /// <summary>
    /// Deserializes an object, and gets its constituent parts via the provided transport.
    /// </summary>
    /// <param name="object"></param>
    /// <param name="transport"></param>
    /// <returns></returns>
    public Base DeserializeAndGet(string @object, ITransport transport)
    {
      Converter.ResetAndInitialize();
      Converter.Transport = transport;

      return JsonConvert.DeserializeObject<Base>(@object, ConversionSettings);
    }
  }

}
