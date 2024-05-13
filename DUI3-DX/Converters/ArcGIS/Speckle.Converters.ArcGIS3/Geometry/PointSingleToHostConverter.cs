using ArcGIS.Desktop.Mapping;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class PointToHostConverter : IRawConversion<SOG.Point, ACG.MapPoint>
{
  private readonly IConversionContextStack<Map, ACG.Unit> _contextStack;

  public PointToHostConverter(IConversionContextStack<Map, ACG.Unit> contextStack)
  {
    _contextStack = contextStack;
  }

  public object Convert(Base target) => RawConvert((SOG.Point)target);

  public ACG.MapPoint RawConvert(SOG.Point target)
  {
    double scaleFactor = Units.GetConversionFactor(
      target.units,
      _contextStack.Current.Document.SpatialReference.Unit.ToString()
    );
    return new ACG.MapPointBuilderEx(
      target.x * scaleFactor,
      target.y * scaleFactor,
      target.z * scaleFactor
    ).ToGeometry();
  }
}
