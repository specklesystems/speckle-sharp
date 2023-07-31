using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Api;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Linq;

namespace DUI3.Utils;

/// <summary>
/// This converter ensures we can do polymorphic deserialization to concrete types. It is automatically added to all
/// serialization settings from <see cref="DUI3.Utils.Serialization.GetSerializerSettings"/>. This converter is intended
/// for use only with UI bound types, not Speckle Bases.
/// </summary>
public class DiscriminatedObjectConverter : JsonConverter<DiscriminatedObject>
{
  public override void WriteJson(JsonWriter writer, DiscriminatedObject value, JsonSerializer serializer)
  {
    serializer.Serialize(writer, value);
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
    var type = GetTypeByName(typeName);
    if (type == null)
    {
      throw new Operations.SpeckleDeserializeException("DUI3 Discriminator converter deserialization failed, type not found: " + typeName);
    }

    var obj = Activator.CreateInstance(type);
    serializer.Populate(jsonObject.CreateReader(), obj);
    return obj as DiscriminatedObject;
  }

  private static readonly Dictionary<string, Type> TypeCache = new Dictionary<string, Type>();
  private Type GetTypeByName(string name)
  {
    TypeCache.TryGetValue(name, out Type myType);
    if (myType != null) return myType;
    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Reverse())
    {
      var type = assembly.DefinedTypes.FirstOrDefault(t => t.FullName != null && t.FullName.Contains(name));
      if (type != null)
      {
        TypeCache[name] = type;
        return type;
      }
    }
    return null;
  }
}
