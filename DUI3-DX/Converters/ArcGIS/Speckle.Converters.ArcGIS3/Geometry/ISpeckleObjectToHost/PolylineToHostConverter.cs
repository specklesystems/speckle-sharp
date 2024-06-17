using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Geometry.ISpeckleObjectToHost;

[NameAndRankValue(nameof(SOG.Polyline), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolylineToHostConverter : IToHostTopLevelConverter, ITypedConverter<SOG.Polyline, ACG.Polyline>
{
  private readonly ITypedConverter<SOG.Point, ACG.MapPoint> _pointConverter;
  private readonly IConversionContextStack<ArcGISDocument, ACG.Unit> _contextStack;

  public PolylineToHostConverter(
    ITypedConverter<SOG.Point, ACG.MapPoint> pointConverter,
    IConversionContextStack<ArcGISDocument, ACG.Unit> contextStack
  )
  {
    _pointConverter = pointConverter;
    _contextStack = contextStack;
  }

  public object Convert(Base target) => Convert((SOG.Polyline)target);

  public ACG.Polyline Convert(SOG.Polyline target)
  {
    List<SOG.Point> originalPts = target.GetPoints();
    var points = originalPts.Select(x => _pointConverter.Convert(x)).ToList();
    if (target.closed && originalPts[0] != originalPts[^1])
    {
      points.Add(points[0]);
    }
    return new ACG.PolylineBuilderEx(
      points,
      ACG.AttributeFlags.HasZ,
      _contextStack.Current.Document.Map.SpatialReference
    ).ToGeometry();
  }
}
