using System.Collections.Generic;
using Autodesk.DesignScript.Runtime;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.ConnectorDynamo.Functions.Developer
{
  public static class Conversion
  {
    /// <summary>
    /// Convert data from Dynamo to their Speckle Base equivalent.
    /// </summary>
    /// <param name="data">Dynamo data</param>
    /// <returns name="base">Base object</returns>
    public static Base ToSpeckle([ArbitraryDimensionArrayImport] object data)
    {
      Analytics.TrackEvent(Analytics.Events.NodeRun, new Dictionary<string, object>() { { "name", "Convert To Speckle" } });
      var converter = new BatchConverter();
      return converter.ConvertRecursivelyToSpeckle(data);
    }

    /// <summary>
    /// Convert data from Speckle's Base object to it`s Dynamo equivalent.
    /// </summary>
    /// <param name="base">Base object</param>
    /// <returns name="data">Dynamo data</returns>
    public static object ToNative(Base @base)
    {
      Analytics.TrackEvent(Analytics.Events.NodeRun, new Dictionary<string, object>() { { "name", "Convert To Native" } });
      var converter = new BatchConverter();
      return converter.ConvertRecursivelyToNative(@base);
    }
  }
}
