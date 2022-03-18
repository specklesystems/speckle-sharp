using System.Collections.Generic;
using Autodesk.DesignScript.Runtime;
using Speckle.Core.Api;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.ConnectorDynamo.Functions.Developer
{
  public static class Serialization
  {
    /// <summary>
    /// Serialize a Speckle Base object to JSON
    /// </summary>
    /// <param name="base">Speckle Base objects to serialize.</param>
    /// <returns name="json">Serialized object in JSON format.</returns>
    public static string Serialize(Base @base)
    {
      Tracker.TrackPageview(Tracker.SERIALIZE);
      Analytics.TrackEvent(Analytics.Events.NodeRun, new Dictionary<string, object>() { { "name", "Serialize" } });
      return Operations.Serialize(@base);
    }

    /// <summary>
    /// Deserialize JSON text to a Speckle Base object
    /// </summary>
    /// <param name="json">Serialized objects in JSON format.</param>
    /// <returns name="base">Deserialized Speckle Base objects.</returns>
    public static object Deserialize(string json)
    {
      Tracker.TrackPageview(Tracker.DESERIALIZE);
      Analytics.TrackEvent(Analytics.Events.NodeRun, new Dictionary<string, object>() { { "name", "Deserialize" } });
      return Operations.Deserialize(json);
    }
  }
}
