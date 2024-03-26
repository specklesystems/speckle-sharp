using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Serialisation;
using Speckle.Newtonsoft.Json;

namespace Speckle.Core.Api;

public static partial class Operations
{
  /// <summary>
  /// Serializes a given object.
  /// </summary>
  /// <remarks>
  /// If you want to save and persist an object to Speckle Transport or Server,
  /// please use any of the "Send" methods.
  /// <see cref="Send(Base,Speckle.Core.Transports.ITransport,bool,System.Action{System.Collections.Concurrent.ConcurrentDictionary{string,int}}?,System.Threading.CancellationToken)"/>
  /// </remarks>
  /// <param name="value">The object to serialise</param>
  /// <param name="cancellationToken"></param>
  /// <returns>A json string representation of the object.</returns>
  public static string Serialize(Base value, CancellationToken cancellationToken = default)
  {
    var serializer = new BaseObjectSerializerV2 { CancellationToken = cancellationToken };
    return serializer.Serialize(value);
  }

  /// <remarks>
  /// Note: if you want to pull an object from a Speckle Transport or Server,
  /// please use
  /// <see cref="Receive(string,Speckle.Core.Transports.ITransport?,Speckle.Core.Transports.ITransport?,System.Action{System.Collections.Concurrent.ConcurrentDictionary{string,int}}?,System.Action{int}?,System.Threading.CancellationToken)"/>
  /// </remarks>
  /// <param name="value">The json string representation of a speckle object that you want to deserialize</param>
  /// <param name="cancellationToken"></param>
  /// <returns><inheritdoc cref="BaseObjectDeserializerV2.Deserialize"/></returns>
  /// <exception cref="ArgumentNullException"><paramref name="value"/> was null</exception>
  /// <exception cref="JsonReaderException "><paramref name="value"/> was not valid JSON</exception>
  /// <exception cref="SpeckleException"><paramref name="value"/> cannot be deserialised to type <see cref="Base"/></exception>
  /// <exception cref="Speckle.Core.Transports.TransportException"><paramref name="value"/> contains closure references (see Remarks)</exception>
  public static Base Deserialize(string value, CancellationToken cancellationToken = default)
  {
    var deserializer = new BaseObjectDeserializerV2 { CancellationToken = cancellationToken };
    return deserializer.Deserialize(value);
  }

  #region obsolete

  [Obsolete("Serializer v1 is deprecated, use other overload(s)")]
  public static string Serialize(
    Base value,
    SerializerVersion serializerVersion,
    CancellationToken cancellationToken = default
  )
  {
    if (serializerVersion == SerializerVersion.V1)
    {
      var (serializer, settings) = GetSerializerInstance();
      serializer.CancellationToken = cancellationToken;

      return JsonConvert.SerializeObject(value, settings);
    }
    else
    {
      return Serialize(value, cancellationToken);
    }
  }

  [Obsolete("Serializer v1 is deprecated, use other overload(s)")]
  public static Base Deserialize(
    string value,
    SerializerVersion serializerVersion,
    CancellationToken cancellationToken = default
  )
  {
    if (serializerVersion == SerializerVersion.V1)
    {
      var (serializer, settings) = GetSerializerInstance();
      serializer.CancellationToken = cancellationToken;
      var ret = JsonConvert.DeserializeObject<Base>(value, settings);
      return ret ?? throw new SpeckleException($"{nameof(value)} failed to deserialize to a {nameof(Base)} object");
    }

    return Deserialize(value, cancellationToken);
  }

  [Obsolete("Please use the Deserialize(string value) function.", true)]
  public static List<Base> DeserializeArray(
    string objectArr,
    SerializerVersion serializerVersion = SerializerVersion.V2
  )
  {
    throw new NotImplementedException();
  }

  [Obsolete(
    "Please use the Deserialize(Base @object) function. This function will be removed in later versions.",
    true
  )]
  public static Dictionary<string, object> DeserializeDictionary(string dictionary)
  {
    throw new NotImplementedException();
  }

  [Obsolete("Use overload that takes cancellation token last")]
  [SuppressMessage("Naming", "CA1720:Identifier contains type name")]
  public static Base Deserialize(
    string @object,
    CancellationToken cancellationToken,
    SerializerVersion serializerVersion = SerializerVersion.V2
  )
  {
    return Deserialize(@object, serializerVersion, cancellationToken);
  }

  [Obsolete("Use overload that takes cancellation token last")]
  [SuppressMessage("Naming", "CA1720:Identifier contains type name")]
  public static string Serialize(
    Base @object,
    CancellationToken cancellationToken,
    SerializerVersion serializerVersion = SerializerVersion.V2
  )
  {
    return Serialize(@object, serializerVersion, cancellationToken);
  }

  /// <summary>
  /// Serializes a list of objects. Note: if you want to save and persist objects to speckle, please use any of the "Send" methods.
  /// </summary>
  /// <param name="objects"></param>
  /// <returns></returns>
  [Obsolete("Please use the Serialize(Base value) function. This function will be removed in later versions.", true)]
  public static string Serialize(List<Base> objects)
  {
    throw new NotImplementedException();
  }

  /// <summary>
  /// Serializes a list of objects. Note: if you want to save and persist objects to speckle, please use any of the "Send" methods.
  /// </summary>
  /// <param name="objects"></param>
  /// <returns></returns>
  [Obsolete("Please use the Serialize(Base value) function. This function will be removed in later versions.")]
  public static string Serialize(Dictionary<string, Base> objects)
  {
    throw new NotImplementedException();
  }
  #endregion
}
