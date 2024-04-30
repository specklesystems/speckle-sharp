using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class MultipointToHostConverter : IRawConversion<List<SOG.Point>, ACG.Multipoint>
{
  private readonly IRawConversion<SOG.Point, ACG.MapPoint> _pointConverter;

  public MultipointToHostConverter(IRawConversion<SOG.Point, ACG.MapPoint> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public ACG.Multipoint RawConvert(List<SOG.Point> target)
  {
    List<ACG.MapPoint> pointList = new();
    foreach (SOG.Point pt in target)
    {
      pointList.Add(_pointConverter.RawConvert(pt));
    }
    if (pointList.Count == 0)
    {
      throw new SpeckleConversionException("Conversion was not successful");
    }
    return new ACG.MultipointBuilderEx(pointList, ACG.AttributeFlags.HasZ).ToGeometry();
  }
}
