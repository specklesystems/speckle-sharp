using System;
using System.Collections.Generic;
using Speckle.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Speckle.Serialisation
{
  public class Serialiser
  {
    public Serialiser()
    {
    }

    public IEnumerable<Base> Serialize(Base @base)
    {
      return null;
    }
  }

  public class BaseObjectSerializer : JsonConverter
  {
    public Serialiser Serialiser;

    public BaseObjectSerializer(Serialiser serialiser)
    {
      Serialiser = serialiser;
    }

    public override bool CanConvert(Type objectType) => true;
    //{
    //  throw new NotImplementedException();
    //}

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    { 
      throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      throw new NotImplementedException();
    }
  }
}
