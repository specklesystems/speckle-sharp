using Autodesk.DesignScript.Runtime;
using Speckle.Core.Api;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.ConnectorDynamo.Functions.Advanced
{
  public static class Serialization
  {
    /// <summary>
    /// Serialize a Base object to JSON
    /// </summary>
    /// <param name="base">Base object</param>
    /// <returns name="json">JSON text</returns>
    public static string Serialize(Base @base)
    {
      Tracker.TrackPageview(Tracker.SERIALIZE);
      return Operations.Serialize(@base);
    }

    /// <summary>
    /// Deserialize JSON text to a Base object
    /// </summary>
    /// <param name="json">JSON text</param>
    /// <returns name="base">Base object</returns>
    public static object Deserialize(string json)
    {
      Tracker.TrackPageview(Tracker.DESERIALIZE);
      return Operations.Deserialize(json);
    }
  }
}
