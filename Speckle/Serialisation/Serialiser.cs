using System;
using System.Collections.Generic;
using Speckle.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Collections;
using Speckle.Transports;
using System.Linq;
using System.Threading.Tasks;

namespace Speckle.Serialisation
{
  /// <summary>
  /// Main serialisation (and persistance) class for speckle objects. Exposes several methods that help with serialisation, simultaneous serialisation and persistance, as well as deserialisation, and simultaneous deserialization and retrieval of objects.
  /// </summary>
  public class Serializer
  {
    public BaseObjectSerializer RawSerializer;

    public JsonSerializerSettings RawSerializerSettings;

    public SqlLiteObjectTransport Transport;

    /// <summary>
    /// Initializes the converter, and sets some default values for newtonsoft. This class exposes several methods that help with serialisation, simultaneous serialisation and persistance, as well as deserialisation, and simultaneous deserialization and retrieval of objects.
    /// </summary>
    public Serializer(SqlLiteObjectTransport transport = null)
    {
      RawSerializer = new BaseObjectSerializer();
      RawSerializerSettings = new JsonSerializerSettings()
      {
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
#if DEBUG
        Formatting = Formatting.Indented,
#else
        Formatting = Formatting.None,
#endif
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        Converters = new List<Newtonsoft.Json.JsonConverter> { RawSerializer }
      };

      if (transport == null)
      {
        Transport = new SqlLiteObjectTransport();
      }
      else
        Transport = transport;
    }

    /// <summary>
    /// Fully serializes an object, and returns its hash.
    /// </summary>
    /// <param name="object"></param>
    /// <returns>The hash of the serialised object.</returns>
    public async Task<string> Serialize(Base @object, Action<string, int> onProgressAction = null)
    {
      RawSerializer.ResetAndInitialize();
      RawSerializer.Transport = Transport;
      RawSerializer.OnProgressAction = onProgressAction;

      var obj =  JsonConvert.SerializeObject(@object, RawSerializerSettings);
      var hash = JObject.Parse(obj).GetValue("hash").ToString();

      await Transport.WriteComplete();
      return hash;
    }

    /// <summary>
    /// Deserializes a fully serialized object. If any references are present, it will fail.
    /// </summary>
    /// <param name="object"></param>
    /// <returns></returns>
    public Base Deserialize(string hash, Action<string, int> onProgressAction = null)
    {
      RawSerializer.ResetAndInitialize();
      RawSerializer.OnProgressAction = onProgressAction;

      var objString = Transport.GetObject(hash);
      return JsonConvert.DeserializeObject<Base>(objString, RawSerializerSettings);
    }

    /// <summary>
    /// Deserializes an object, and gets its constituent parts via the provided transport.
    /// </summary>
    /// <param name="object"></param>
    /// <param name="transport"></param>
    /// <returns></returns>
    public Base DeserializeAndGet(string @object, ITransport transport, Action<string, int> onProgressAction = null)
    {
      RawSerializer.ResetAndInitialize();
      RawSerializer.Transport = transport;
      RawSerializer.OnProgressAction = onProgressAction;

      return JsonConvert.DeserializeObject<Base>(@object, RawSerializerSettings);
    }
  }

}
