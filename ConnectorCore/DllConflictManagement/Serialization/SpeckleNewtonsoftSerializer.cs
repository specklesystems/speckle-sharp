using Speckle.Newtonsoft.Json;

namespace Speckle.DllConflictManagement.Serialization;

public class SpeckleNewtonsoftSerializer : ISerializer
{
  public string Serialize<T>(T obj) => JsonConvert.SerializeObject(obj);

  public T Deserialize<T>(string serialized) => JsonConvert.DeserializeObject<T>(serialized);
}
