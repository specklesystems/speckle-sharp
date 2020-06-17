using System;
using System.Collections.Generic;
using Speckle.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Collections;
using Speckle.Transports;
using System.Linq;

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
    /// <param name="object"></param>
    /// <returns></returns>
    public string Serialize(Base @object)
    {
      Converter.ResetAndInitialize();
      var obj =  JsonConvert.SerializeObject(@object, ConversionSettings);
      var hash = JObject.Parse(obj).GetValue("hash").ToString();
      return obj;
    }

    /// <summary>
    /// Serializes an object, and persists its constituent parts via the provided transport.
    /// </summary>
    /// <param name="object"></param>
    /// <param name="transport">Transport that will be "waited on" during serialisation.</param>
    /// <param name="onProgressAction">Action that will be executed as the serializer progresses.</param>
    /// <returns></returns>
    public string SerializeAndSave(Base @object, ITransport transport = null, Action<string> onProgressAction = null)
    {
      if (transport == null)
        throw new Exception("You must provide at least one transport.");

      // set up things
      Converter.ResetAndInitialize();
      Converter.Transport = transport;
      Converter.OnProgressAction = onProgressAction;

      return JsonConvert.SerializeObject(@object, ConversionSettings);
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
    public Base DeserializeAndGet(string @object, ITransport transport, Action<string> onProgressAction = null)
    {
      Converter.ResetAndInitialize();
      Converter.Transport = transport;
      Converter.OnProgressAction = onProgressAction;

      return JsonConvert.DeserializeObject<Base>(@object, ConversionSettings);
    }
  }

}
