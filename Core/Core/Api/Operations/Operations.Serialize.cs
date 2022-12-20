using System;
using System.Collections.Generic;
using System.Threading;
using Speckle.Core.Models;
using Speckle.Core.Serialisation;
using Speckle.Newtonsoft.Json;

namespace Speckle.Core.Api
{
  // TODO: cleanup a bit
  public static partial class Operations
  {

    /// <summary>
    /// Serializes a given object. Note: if you want to save and persist an object to a Speckle Transport or Server, please use any of the "Send" methods. See <see cref="Send(Base, List{Transports.ITransport}, bool, Action{System.Collections.Concurrent.ConcurrentDictionary{string, int}}, Action{string, Exception})"/>.
    /// </summary>
    /// <param name="object"></param>
    /// <returns>A json string representation of the object.</returns>
    public static string Serialize(Base @object)
    {
      return Serialize(@object, CancellationToken.None);
    }

    /// <summary>
    /// Serializes a given object. Note: if you want to save and persist an object to Speckle Transport or Server, please use any of the "Send" methods. See <see cref="Send(Base, List{Transports.ITransport}, bool, Action{System.Collections.Concurrent.ConcurrentDictionary{string, int}}, Action{string, Exception})"/>.
    /// </summary>
    /// <param name="object"></param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>A json string representation of the object.</returns>
    public static string Serialize(Base @object, CancellationToken cancellationToken, SerializerVersion serializerVersion = SerializerVersion.V2)
    {
      if (serializerVersion == SerializerVersion.V1)
      {
        var (serializer, settings) = GetSerializerInstance();
        serializer.CancellationToken = cancellationToken;

        return JsonConvert.SerializeObject(@object, settings);
      }
      else
      {
        var serializer = new BaseObjectSerializerV2();
        serializer.CancellationToken = cancellationToken;
        return serializer.Serialize(@object);
      }
    }


    /// <summary>
    /// Serializes a list of objects. Note: if you want to save and persist objects to speckle, please use any of the "Send" methods.
    /// </summary>
    /// <param name="objects"></param>
    /// <returns></returns>
    [Obsolete("Please use the Serialize(Base @object) function. This function will be removed in later versions.")]
    public static string Serialize(List<Base> objects)
    {
      var (_, settings) = GetSerializerInstance();
      return JsonConvert.SerializeObject(objects, settings);
    }

    /// <summary>
    /// Serializes a list of objects. Note: if you want to save and persist objects to speckle, please use any of the "Send" methods.
    /// </summary>
    /// <param name="objects"></param>
    /// <returns></returns>
    [Obsolete("Please use the Serialize(Base @object) function. This function will be removed in later versions.")]
    public static string Serialize(Dictionary<string, Base> objects)
    {
      var (_, settings) = GetSerializerInstance();
      return JsonConvert.SerializeObject(objects, settings);
    }

    /// <summary>
    /// Deserializes a given object. Note: if you want to pull an object from a Speckle Transport or Server, please use any of the <see cref="Receive(string, Transports.ITransport, Transports.ITransport, Action{System.Collections.Concurrent.ConcurrentDictionary{string, int}})"/>.
    /// </summary>
    /// <param name="object">The json string representation of a speckle object that you want to deserialise.</param>
    /// <returns></returns>
    public static Base Deserialize(string @object)
    {
      return Deserialize(@object, CancellationToken.None);
    }

    /// <summary>
    /// Deserializes a given object. Note: if you want to pull an object from a Speckle Transport or Server, please use any of the <see cref="Receive(string, Transports.ITransport, Transports.ITransport, Action{System.Collections.Concurrent.ConcurrentDictionary{string, int}})"/>.
    /// </summary>
    /// <param name="object">The json string representation of a speckle object that you want to deserialise.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns></returns>
    public static Base Deserialize(string @object, CancellationToken cancellationToken, SerializerVersion serializerVersion = SerializerVersion.V2)
    {
      if (serializerVersion == SerializerVersion.V1)
      {
        var (serializer, settings) = GetSerializerInstance();
        serializer.CancellationToken = cancellationToken;
        return JsonConvert.DeserializeObject<Base>(@object, settings);
      }
      else
      {
        var deserializer = new BaseObjectDeserializerV2();
        deserializer.CancellationToken = cancellationToken;
        return deserializer.Deserialize(@object);
      }
    }

    /// <summary>
    /// Deserializes a list of objects into an array. Note: if you want to pull an object from speckle (either local or remote), please use any of the "Receive" methods.
    /// </summary>
    /// <param name="objectArr"></param>
    /// <returns></returns>
    [Obsolete("Please use the Deserialize(Base @object) function. This function will be removed in later versions.")]
    public static List<Base> DeserializeArray(string objectArr, SerializerVersion serializerVersion = SerializerVersion.V2)
    {
      if (serializerVersion == SerializerVersion.V1)
      {
        var (_, settings) = GetSerializerInstance();
        return JsonConvert.DeserializeObject<List<Base>>(objectArr, settings);
      }
      else
      {
        var deserializer = new BaseObjectDeserializerV2();
        List<object> deserialized = deserializer.DeserializeTransportObject(objectArr) as List<object>;
        List<Base> ret = new List<Base>();
        foreach (object obj in deserialized)
          ret.Add((Base)obj);
        return ret;
      }
    }

    /// <summary>
    /// Deserializes a dictionary object. Note: if you want to pull an object from speckle (either local or remote), please use any of the "Receive" methods.
    /// </summary>
    /// <param name="dictionary"></param>
    /// <returns></returns>
    [Obsolete("Please use the Deserialize(Base @object) function. This function will be removed in later versions.")]
    public static Dictionary<string, object> DeserializeDictionary(string dictionary)
    {
      var (_, settings) = GetSerializerInstance();
      return JsonConvert.DeserializeObject<Dictionary<string, object>>(dictionary, settings);
    }
  }
}
