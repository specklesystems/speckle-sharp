using System.Text.Json;

namespace Speckle.DllConflictManagement.Serialization;

/// <summary>
/// Just a wrapper around the System.Text.Json serializer. If we decide that we want to switch to newtonsoft
/// or even something that is not json-based, then we only need to make a new implementation of the ISerializer
/// </summary>
public class SystemTextJsonSerializer : ISerializer
{
  private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

  public string Serialize<T>(T obj) => JsonSerializer.Serialize(obj, _jsonSerializerOptions);

  public T Deserialize<T>(string serialized) => JsonSerializer.Deserialize<T>(serialized);
}
