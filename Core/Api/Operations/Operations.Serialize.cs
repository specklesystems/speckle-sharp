using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Speckle.Core.Models;

namespace Speckle.Core.Api
{
  // TODO: cleanup a bit
  public static partial class Operations
  {
    /// <summary>
    /// Serializes a given object. Note: if you want to save and persist an object to speckle, please use any of the "Send" methods.
    /// </summary>
    /// <param name="object"></param>
    /// <returns></returns>
    public static string Serialize(Base @object)
    {
      var (_, settings) = GetSerializerInstance();
      return JsonConvert.SerializeObject(@object, settings);
    }

    /// <summary>
    /// Serializes a list of objects. Note: if you want to save and persist objects to speckle, please use any of the "Send" methods.
    /// </summary>
    /// <param name="objects"></param>
    /// <returns></returns>
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
    public static string Serialize(Dictionary<string,Base> objects)
    {
      var (_, settings) = GetSerializerInstance();
      return JsonConvert.SerializeObject(objects, settings);
    }

    /// <summary>
    /// Deserializes a given object. Note: if you want to pull an object from speckle (either local or remote), please use any of the "Receive" methods.
    /// </summary>
    /// <param name="object"></param>
    /// <returns></returns>
    public static Base Deserialize(string @object)
    {
      var (_, settings) = GetSerializerInstance();
      return JsonConvert.DeserializeObject<Base>(@object, settings);
    }

    /// <summary>
    /// Deserializes a list of objects into an array. Note: if you want to pull an object from speckle (either local or remote), please use any of the "Receive" methods.
    /// </summary>
    /// <param name="objectArr"></param>
    /// <returns></returns>
    public static List<Base> DeserializeArray(string objectArr)
    {
      var (_, settings) = GetSerializerInstance();
      return JsonConvert.DeserializeObject<List<Base>>(objectArr, settings);
    }

    /// <summary>
    /// Deserializes a dictionary object. Note: if you want to pull an object from speckle (either local or remote), please use any of the "Receive" methods.
    /// </summary>
    /// <param name="dictionary"></param>
    /// <returns></returns>
    public static Dictionary<string,object> DeserializeDictionary(string dictionary)
    {
      var (_, settings) = GetSerializerInstance();
      return JsonConvert.DeserializeObject<Dictionary<string, object>>(dictionary, settings);
    }
  }
}
