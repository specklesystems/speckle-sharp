namespace Speckle.DllConflictManagement.Serialization;

/// <summary>
/// Serialization and deserialization contract
/// </summary>
public interface ISerializer
{
  public string Serialize<T>(T obj);

  public T Deserialize<T>(string serialized);
}
