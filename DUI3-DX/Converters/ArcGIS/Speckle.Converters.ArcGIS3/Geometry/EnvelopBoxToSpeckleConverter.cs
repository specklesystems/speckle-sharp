using ArcGIS.Core.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using Objects.Primitive;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class EnvelopToSpeckleConverter : ITypedConverter<Envelope, SOG.Box>
{
  private readonly IConversionContextStack<ArcGISDocument, Unit> _contextStack;
  private readonly ITypedConverter<MapPoint, SOG.Point> _pointConverter;

  public EnvelopToSpeckleConverter(
    IConversionContextStack<ArcGISDocument, Unit> contextStack,
    ITypedConverter<MapPoint, SOG.Point> pointConverter
  )
  {
    _contextStack = contextStack;
    _pointConverter = pointConverter;
  }

  public SOG.Box Convert(Envelope target)
  {
    MapPoint pointMin = new MapPointBuilderEx(
      target.XMin,
      target.YMin,
      target.ZMin,
      _contextStack.Current.Document.Map.SpatialReference
    ).ToGeometry();
    MapPoint pointMax = new MapPointBuilderEx(
      target.XMax,
      target.YMax,
      target.ZMax,
      _contextStack.Current.Document.Map.SpatialReference
    ).ToGeometry();
    SOG.Point minPtSpeckle = _pointConverter.Convert(pointMin);
    SOG.Point maxPtSpeckle = _pointConverter.Convert(pointMax);

    var units = _contextStack.Current.SpeckleUnits;

    return new(
      new SOG.Plane(
        minPtSpeckle,
        new SOG.Vector(0, 0, 1, units),
        new SOG.Vector(1, 0, 0, units),
        new SOG.Vector(0, 1, 0, units),
        units
      ),
      new Interval(minPtSpeckle.x, maxPtSpeckle.x),
      new Interval(minPtSpeckle.y, maxPtSpeckle.y),
      new Interval(minPtSpeckle.z, maxPtSpeckle.z),
      _contextStack.Current.SpeckleUnits
    );
  }
}
