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
  /// <summary>
  /// Main serialisation (and persistance) class for speckle objects. Exposes several methods that help with serialisation, simultaneous serialisation and persistance, as well as deserialisation, and simultaneous deserialization and retrieval of objects.
  /// </summary>
  public class Serializer
  {
    public BaseObjectSerializer Converter;

    public JsonSerializerSettings ConversionSettings;

    /// <summary>
    /// Initializes the converter, and sets some default values for newtonsoft. This class exposes several methods that help with serialisation, simultaneous serialisation and persistance, as well as deserialisation, and simultaneous deserialization and retrieval of objects.
    /// </summary>
    public Serializer()
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
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        Converters = new List<Newtonsoft.Json.JsonConverter> { Converter }
      };
    }

    /// <summary>
    /// Fully serializes an object, and returns its string representation.
    /// </summary>
    /// <param name="objects"></param>
    /// <returns></returns>
    public string Serialize(Base objects)
    {
      Converter.ResetAndInitialize();
      return JsonConvert.SerializeObject(objects, ConversionSettings);
    }

    public IEnumerable<string> Serialize(IEnumerable<Base> @objects)
    {
      foreach (var obj in objects)
        yield return Serialize(obj);
    }
    
    /// <summary>
    /// Serializes an object, and persists its constituent parts via the provided transport.
    /// </summary>
    /// <param name="object"></param>
    /// <param name="transport"></param>
    /// <returns></returns>
    public string SerializeAndSave(Base @object, ITransport transport)
    {
      Converter.ResetAndInitialize();
      Converter.Transport = transport;

      return JsonConvert.SerializeObject(@object, ConversionSettings);
    }

    public IEnumerable<string> SerializeAndSave(IEnumerable<Base> objects, ITransport transport)
    {
      List<string> results = new List<string>();
      foreach (var obj in objects)
        results.Add( SerializeAndSave(obj, transport) );
      return results;
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
