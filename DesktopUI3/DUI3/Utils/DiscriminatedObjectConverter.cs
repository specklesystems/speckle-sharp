using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Speckle.Core.Logging;
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
  private readonly JsonSerializer _localSerializer =
    new()
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

    var typeName =
      jsonObject.Value<string>("typeDiscriminator")
      ?? throw new Speckle.Core.Serialisation.SpeckleDeserializeException(
        "DUI3 Discriminator converter deserialization failed: did not find a typeDiscriminator field."
      );
    var type =
      GetTypeByName(typeName)
      ?? throw new Speckle.Core.Serialisation.SpeckleDeserializeException(
        "DUI3 Discriminator converter deserialization failed, type not found: " + typeName
      );
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

  private static readonly Dictionary<string, Type> s_typeCache = new();

  private Type GetTypeByName(string name)
  {
    s_typeCache.TryGetValue(name, out Type myType);
    if (myType != null)
    {
      return myType;
    }

    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Reverse())
    {
      try
      {
        // assembly.
        var allAssemblyTypes = assembly.DefinedTypes;
        var assemblyTypesEnumerable = allAssemblyTypes.ToList();
        if (assemblyTypesEnumerable.Any())
        {
          var type = assemblyTypesEnumerable.FirstOrDefault(t => t.FullName != null && t.FullName.Contains(name));

          if (type != null)
          {
            s_typeCache[name] = type;
            return type;
          }
        }
      }
      catch (Exception e) // DO NOT CHANGE THIS TO SPECKLE EXCEPTION
      {
        Debug.WriteLine(e.Message);
      }
    }
    return null;
  }
}

public class AbstractConverter<TReal, TAbstract> : JsonConverter
{
  public override bool CanConvert(Type objectType) => objectType == typeof(TAbstract);

  public override object ReadJson(
    JsonReader reader,
    Type objectType,
    object existingValue,
    JsonSerializer serializer
  ) => serializer.Deserialize<TReal>(reader);

  public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
    serializer.Serialize(writer, value);
}
