using Autodesk.DesignScript.Runtime;
using Speckle.Core.Models;

namespace Speckle.ConnectorDynamo.Functions.Advanced
{
  public static class Conversion
  {
    /// <summary>
    /// Convert data from Dynamo to Speckle's Base object
    /// </summary>
    /// <param name="data">Dynamo data</param>
    /// <returns name="base">Base object</returns>
    public static Base ToSpeckle([ArbitraryDimensionArrayImport] object data)
    {
      var converter = new BatchConverter();
      return converter.ConvertRecursivelyToSpeckle(data);
    }

    /// <summary>
    /// Convert data from Speckle's Base object to Dynamo
    /// </summary>
    /// <param name="base">Base object</param>
    /// <returns name="data">Dynamo data</returns>
    public static object ToNative(Base @base)
    {
      var converter = new BatchConverter();
      return converter.ConvertRecursivelyToNative(@base);
    }
  }
}
