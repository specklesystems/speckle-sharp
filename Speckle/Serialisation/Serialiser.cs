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

    //public MemoryTransport DefaultTransport = new MemoryTransport();

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

    public string Serialize(Base @base)
    {
      Converter.ResetAndInitialize();
      Converter.SessionTransport = new MemoryTransport();

      JsonConvert.SerializeObject(@base, ConversionSettings);

      return "[" + String.Join(",", Converter.SessionTransport.GetAllObjects()) + "]";
    }

    public string SerializeAndSave(Base @base, ITransport transport)
    {
      Converter.ResetAndInitialize();
      Converter.SessionTransport = new MemoryTransport();
      Converter.Transport = new MemoryTransport(); // HACK, to remove

      JsonConvert.SerializeObject(@base, ConversionSettings);

      return JsonConvert.SerializeObject(Converter.SessionTransport.GetAllObjects());
    }

    public IEnumerable<Base> Deserialize(string objects)
    {
      return null;
    }

    public IEnumerable<Base> DeserializeAndGet(string objects, ITransport transport)
    {
      return null;
    }
  }

}
