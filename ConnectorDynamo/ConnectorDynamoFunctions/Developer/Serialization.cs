using Speckle.Core.Api;
using Speckle.Core.Models;

namespace Speckle.ConnectorDynamo.Functions.Developer;

public static class Serialization
{
  /// <summary>
  /// Serialize a Speckle Base object to JSON
  /// </summary>
  /// <param name="base">Speckle Base objects to serialize.</param>
  /// <returns name="json">Serialized object in JSON format.</returns>
  public static string Serialize(Base @base)
  {
    AnalyticsUtils.TrackNodeRun("Serialize");

    return Operations.Serialize(@base);
  }

  /// <summary>
  /// Deserialize JSON text to a Speckle Base object
  /// </summary>
  /// <param name="json">Serialized objects in JSON format.</param>
  /// <returns name="base">Deserialized Speckle Base objects.</returns>
  public static object Deserialize(string json)
  {
    AnalyticsUtils.TrackNodeRun("Deserialize");

    return Operations.Deserialize(json);
  }
}
