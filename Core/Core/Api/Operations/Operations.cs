using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Speckle.Core.Serialisation;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Serialization;

namespace Speckle.Core.Api;

/// <summary>
/// Exposes several key methods for interacting with Speckle.Core.
/// <para>Serialize/Deserialize</para>
/// <para>Push/Pull (methods to serialize & send data to one or more servers)</para>
/// </summary>
public static partial class Operations
{
  /// <summary>
  /// Convenience method to instantiate an instance of the default object serializer and settings pre-populated with it.
  /// </summary>
  public static (BaseObjectSerializer, JsonSerializerSettings) GetSerializerInstance()
  {
    var serializer = new BaseObjectSerializer();
    var settings = new JsonSerializerSettings
    {
      NullValueHandling = NullValueHandling.Ignore,
      ContractResolver = new CamelCasePropertyNamesContractResolver(),
      Formatting = Formatting.None,
      ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
      Converters = new List<JsonConverter> { serializer }
    };

    return (serializer, settings);
  }

  /// <summary>
  /// Factory for progress actions used internally inside send & receive methods.
  /// </summary>
  /// <param name="localProgressDict"></param>
  /// <param name="onProgressAction"></param>
  /// <returns></returns>
  private static Action<string, int> GetInternalProgressAction(
    ConcurrentDictionary<string, int> localProgressDict,
    Action<ConcurrentDictionary<string, int>> onProgressAction = null
  )
  {
    return (name, processed) =>
    {
      if (localProgressDict.ContainsKey(name))
        localProgressDict[name] += processed;
      else
        localProgressDict[name] = processed;
      onProgressAction?.Invoke(localProgressDict);
    };
  }
}
