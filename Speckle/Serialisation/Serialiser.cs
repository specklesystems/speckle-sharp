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
      return JsonConvert.SerializeObject(@object, ConversionSettings);
    }

    /// <summary>
    /// Serializes an object, and persists its constituent parts via the provided transport.
    /// </summary>
    /// <param name="object"></param>
    /// <param name="syncTransport">Transport that will be "waited on" during serialisation.</param>
    /// <param name="asyncTransports">Transports that will not be waited on to complete (ie, remote http based ones)</param>
    /// <param name="OnProgressAction">Action that will be executed as </param>
    /// <returns></returns>
    public string SerializeAndSave(Base @object, ITransport syncTransport = null, IEnumerable<ITransport> asyncTransports = null, Action<string> OnProgressAction = null)
    {
      if (syncTransport == null && asyncTransports?.Count() == 0)
        throw new Exception("You must provide at least one transport.");

      // set up things
      Converter.ResetAndInitialize();

      Converter.SyncTransport = syncTransport;

      if (asyncTransports != null && asyncTransports.Count() > 0)
        Converter.AsyncTransports = asyncTransports.ToList();

      Converter.OnProgressAction = OnProgressAction;

      return JsonConvert.SerializeObject(@object, ConversionSettings);
    }

    public string SerializeAndSave(Base @object, IEnumerable<ITransport> transports)
    {

      return null;
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
      Converter.SyncTransport = transport;

      return JsonConvert.DeserializeObject<Base>(@object, ConversionSettings);
    }
  }

}
