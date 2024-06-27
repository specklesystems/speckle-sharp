using ArcGIS.Core.Geometry;

namespace Speckle.Converters.ArcGIS3.Utils;

/// <summary>
/// Container with origin offsets and rotation angle for the specified SpatialReference
/// </summary>
public struct CRSoffsetRotation
{
  public SpatialReference SpatialReference { get; }
  public double LatOffset { get; set; }
  public double LonOffset { get; set; }
  public double TrueNorthRadians { get; set; }

  private void NormalizeAngle()
  {
    if (TrueNorthRadians < -2 * Math.PI || TrueNorthRadians > 2 * Math.PI)
    {
      // do something
      TrueNorthRadians += 0;
    }
  }

  public SOG.Point OffsetRotateOnReceive(SOG.Point point)
  {
    // ?? scale PT first, to match CRS?
    NormalizeAngle();
    double x2 = point.x * Math.Cos(TrueNorthRadians) - point.y * Math.Sin(TrueNorthRadians);
    double y2 = point.x * Math.Sin(TrueNorthRadians) + point.y * Math.Cos(TrueNorthRadians);
    x2 += LonOffset;
    y2 += LatOffset;
    return new SOG.Point(x2, y2, point.z, point.units);
  }

  public SOG.Point OffsetRotateOnSend(SOG.Point point)
  {
    // ?? scale PT first, to match CRS?
    NormalizeAngle();
    double x2 = point.x - LonOffset;
    double y2 = point.y - LatOffset;
    x2 = x2 * Math.Cos(TrueNorthRadians) + y2 * Math.Sin(TrueNorthRadians);
    y2 = -x2 * Math.Sin(TrueNorthRadians) + y2 * Math.Cos(TrueNorthRadians);
    return new SOG.Point(x2, y2, point.z, point.units);
  }

  /// <summary>
  /// Initializes a new instance of <see cref="CRSoffsetRotation"/>.
  /// </summary>
  /// <param name="spatialReference">SpatialReference to apply offsets and rotation to.</param>
  public CRSoffsetRotation(SpatialReference spatialReference)
  {
    SpatialReference = spatialReference;
    LatOffset = 0;
    LonOffset = 0;
    TrueNorthRadians = 0;
  }

  /// <summary>
  /// Initializes a new instance of <see cref="CRSoffsetRotation"/>.
  /// </summary>
  /// <param name="spatialReference">SpatialReference to apply offsets and rotation to.</param>
  /// <param name="latOffset">Latitude (Y) ofsset in the current SpatialReference units.</param>
  /// <param name="lonOffset">Longitude (X) ofsset in the current SpatialReference units.</param>
  /// <param name="trueNorthRadians">Angle to True North in radians.</param>
  public CRSoffsetRotation(
    SpatialReference spatialReference,
    double latOffset,
    double lonOffset,
    double trueNorthRadians
  )
  {
    SpatialReference = spatialReference;
    LatOffset = latOffset;
    LonOffset = lonOffset;
    TrueNorthRadians = trueNorthRadians;
  }
}
