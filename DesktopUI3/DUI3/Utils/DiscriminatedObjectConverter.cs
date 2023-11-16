using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Api;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Linq;
using Speckle.Newtonsoft.Json.Serialization;

namespace DUI3.Utils;

/// <summary>
/// This converter ensures we can do polymorphic deserialization to concrete types. It is automatically added to all
/// serialization settings from <see cref="SerializationSettingsFactory.GetSerializerSettings"/>. This converter is intended
/// for use only with UI bound types, not Speckle Bases.
/// </summary>
public class DiscriminatedObjectConverter : JsonConverter<DiscriminatedObject>
{
  private readonly JsonSerializer _localSerializer = new JsonSerializer
  {
    DefaultValueHandling = DefaultValueHandling.Ignore,
    ContractResolver = new CamelCasePropertyNamesContractResolver(), 
    NullValueHandling = NullValueHandling.Ignore
  };

  public override void WriteJson(JsonWriter writer, DiscriminatedObject value, JsonSerializer serializer)
  {
    var jo = JObject.FromObject(value, _localSerializer);
    jo.WriteTo(writer);
  }

  public override DiscriminatedObject ReadJson(
    JsonReader reader,
    Type objectType,
    DiscriminatedObject existingValue,
    bool hasExistingValue,
    JsonSerializer serializer
  )
  {
    JObject jsonObject = JObject.Load(reader);
    
    var typeName = jsonObject.Value<string>("typeDiscriminator");
    if (typeName == null)
    {
      throw new Speckle.Core.Api.Operations.SpeckleDeserializeException("DUI3 Discriminator converter deserialization failed: did not find a typeDiscriminator field.");
    }
    
    var type = GetTypeByName(typeName);
    if (type == null)
    {
      throw new Speckle.Core.Api.Operations.SpeckleDeserializeException("DUI3 Discriminator converter deserialization failed, type not found: " + typeName);
    }

    var obj = Activator.CreateInstance(type);
    serializer.Populate(jsonObject.CreateReader(), obj);
    
    // Store the JSON property names in the object for later comparison
    if (obj is PropertyValidator pv)
    {
      // Capture property names from JSON
      var jsonPropertyNames = jsonObject.Properties().Select(p => p.Name).ToList();
      
      pv.JsonPropertyNames = jsonPropertyNames;
    }
    
    return obj as DiscriminatedObject;
  }

  private static readonly Dictionary<string, Type> TypeCache = new Dictionary<string, Type>();
  private Type GetTypeByName(string name)
  {
    TypeCache.TryGetValue(name, out Type myType);
    if (myType != null) return myType;
    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Reverse())
    {
      try
      {
        var type = assembly.DefinedTypes.FirstOrDefault(t => t.FullName != null && t.FullName.Contains(name));
        if (type != null)
        {
          TypeCache[name] = type;
          return type;
        }
      }
      catch
      {
        
      }
    }
    return null;
  }
}

public class AbstractConverter<TReal, TAbstract> 
  : JsonConverter
{
  public override bool CanConvert(Type objectType)
  {
    return objectType == typeof(TAbstract);
  }

  public override object ReadJson(JsonReader reader, Type type, object value, JsonSerializer jser)
  {
    return jser.Deserialize<TReal>(reader);
  }

  public override void WriteJson(JsonWriter writer, object value, JsonSerializer jser)
  {
    jser.Serialize(writer, value);
  }
}
