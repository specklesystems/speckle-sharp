using System.Collections.Generic;

namespace Speckle.Converters.Autocad.Extensions;

public static class ListExtensions
{
  public static SOG.Polyline ConvertToSpecklePolyline(this List<double> pointList, string speckleUnits)
  {
    // throw if list is malformed
    if (pointList.Count % 3 != 0)
    {
      throw new System.ArgumentException("Point list of xyz values is malformed.");
    }

    return new(pointList, speckleUnits);
  }
}
