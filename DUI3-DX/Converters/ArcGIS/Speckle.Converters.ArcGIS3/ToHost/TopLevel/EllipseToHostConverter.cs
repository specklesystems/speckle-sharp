using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Ellipse), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class EllipseToHostConverter : IToHostTopLevelConverter, ITypedConverter<SOG.Ellipse, ACG.Polyline>
{
  private readonly ITypedConverter<SOG.Point, ACG.MapPoint> _pointConverter;
  private readonly IConversionContextStack<ArcGISDocument, ACG.Unit> _contextStack;

  public EllipseToHostConverter(
    ITypedConverter<SOG.Point, ACG.MapPoint> pointConverter,
    IConversionContextStack<ArcGISDocument, ACG.Unit> contextStack
  )
  {
    _pointConverter = pointConverter;
    _contextStack = contextStack;
  }

  public object Convert(Base target) => Convert((SOG.Ellipse)target);

  public ACG.Polyline Convert(SOG.Ellipse target)
  {
    // dummy check
    if (target.firstRadius == null || target.secondRadius == null)
    {
      throw new ArgumentException("Ellipse is missing the first or second radius");
    }
    if (
      target.plane.normal.x != 0 || target.plane.normal.y != 0 || target.plane.xdir.z != 0 || target.plane.ydir.z != 0
    )
    {
      throw new ArgumentException("Only Ellipses in XY plane are supported");
    }

    ACG.MapPoint centerPt = _pointConverter.Convert(target.plane.origin);
    double scaleFactor = Units.GetConversionFactor(target.units, _contextStack.Current.SpeckleUnits);

    // set default values
    double angle = Math.Atan2(target.plane.xdir.y, target.plane.xdir.x);
    double majorAxisRadius = (double)target.firstRadius;
    double minorAxisRatio = (double)target.secondRadius / majorAxisRadius;

    // adjust if needed
    if (minorAxisRatio > 1)
    {
      majorAxisRadius = (double)target.secondRadius;
      minorAxisRatio = 1 / minorAxisRatio;
      angle += Math.PI / 2;
    }

    ACG.EllipticArcSegment segment = ACG.EllipticArcBuilderEx.CreateEllipse(
      new ACG.Coordinate2D(centerPt),
      angle,
      majorAxisRadius * scaleFactor,
      minorAxisRatio,
      ACG.ArcOrientation.ArcCounterClockwise,
      _contextStack.Current.Document.Map.SpatialReference
    );

    return new ACG.PolylineBuilderEx(
      segment,
      ACG.AttributeFlags.HasZ,
      _contextStack.Current.Document.Map.SpatialReference
    ).ToGeometry();
  }
}