using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Converters.Common;
using System.Collections.Generic;

namespace Speckle.Converters.Autocad.Extensions;

public static class ListExtensions
{
  public static SOG.Polyline ConvertToSpecklePolyline(
    this List<double> pointList,
    IConversionContextStack<Document, UnitsValue> contextStack
  )
  {
    // throw if list is malformed
    if (pointList.Count % 3 != 0)
    {
      throw new System.ArgumentException("Point list of xyz values is malformed.");
    }

    return new(pointList, contextStack.Current.SpeckleUnits);
  }
}
