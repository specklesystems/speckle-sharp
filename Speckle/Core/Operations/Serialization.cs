using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Speckle.Models;

namespace Speckle.Core
{
  public static partial class Operations
  {
    #region Classic serialization
    /// <summary>
    /// Serializes a given object. Note: if you want to save and persist an object to speckle, please use any of the "Push" methods.
    /// </summary>
    /// <param name="object"></param>
    /// <returns></returns>
    public static string Serialize(Base @object)
    {
      var (_, settings) = GetSerializerInstance();
      return JsonConvert.SerializeObject(@object, settings);
    }

    /// <summary>
    /// Serializes a list of objects. Note: if you want to save and persist objects to speckle, please use any of the "Push" methods.
    /// </summary>
    /// <param name="objects"></param>
    /// <returns></returns>
    public static string Serialize(List<Base> objects)
    {
      var (_, settings) = GetSerializerInstance();
      return JsonConvert.SerializeObject(objects, settings);
    }

    /// <summary>
    /// Deserializes a given object. 
    /// </summary>
    /// <param name="object"></param>
    /// <returns></returns>
    public static Base Deserialize(string @object)
    {
      var (_, settings) = GetSerializerInstance();
      return JsonConvert.DeserializeObject<Base>(@object, settings);
    }

    public static List<Base> DeserializeArray(string @object)
    {
      var (_, settings) = GetSerializerInstance();
      return JsonConvert.DeserializeObject<List<Base>>(@object, settings);
    }
    #endregion
  }
}