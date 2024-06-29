using Speckle.Core.Kits;

namespace Speckle.Converters.ArcGIS3.Utils;

/// <summary>
/// Container with origin offsets and rotation angle for the specified SpatialReference
/// </summary>
public struct CRSoffsetRotation
{
  public ACG.SpatialReference SpatialReference { get; }
  public string SpeckleUnitString { get; set; }
  public double LatOffset { get; set; }
  public double LonOffset { get; set; }
  public double TrueNorthRadians { get; set; }

  public SOG.Point OffsetRotateOnReceive(SOG.Point point)
  {
    // scale point to match units of the SpatialReference
    string originalUnits = point.units;
    point = ScalePoint(point, originalUnits, SpeckleUnitString);

    NormalizeAngle();
    double x2 = point.x * Math.Cos(TrueNorthRadians) - point.y * Math.Sin(TrueNorthRadians);
    double y2 = point.x * Math.Sin(TrueNorthRadians) + point.y * Math.Cos(TrueNorthRadians);
    x2 += LonOffset;
    y2 += LatOffset;
    SOG.Point movedPoint = new(x2, y2, point.z, SpeckleUnitString);

    // scale back to original units
    movedPoint = ScalePoint(movedPoint, SpeckleUnitString, originalUnits);

    return movedPoint;
  }

  public SOG.Point OffsetRotateOnSend(SOG.Point point)
  {
    // scale point to match units of the SpatialReference
    string originalUnits = point.units;
    point = ScalePoint(point, originalUnits, SpeckleUnitString);

    // rotate and move point
    NormalizeAngle();
    double x2 = point.x - LonOffset;
    double y2 = point.y - LatOffset;
    x2 = x2 * Math.Cos(TrueNorthRadians) + y2 * Math.Sin(TrueNorthRadians);
    y2 = -x2 * Math.Sin(TrueNorthRadians) + y2 * Math.Cos(TrueNorthRadians);
    SOG.Point movedPoint = new(x2, y2, point.z, SpeckleUnitString);

    // scale back to original units
    movedPoint = ScalePoint(movedPoint, SpeckleUnitString, originalUnits);

    return movedPoint;
  }

  private readonly SOG.Point ScalePoint(SOG.Point point, string fromUnit, string toUnit)
  {
    double scaleFactor = Units.GetConversionFactor(fromUnit, toUnit);
    return new SOG.Point(point.x * scaleFactor, point.x * scaleFactor, point.z * scaleFactor, toUnit);
  }

  private readonly string GetSpeckleUnit(ACG.SpatialReference spatialReference)
  {
    return new ArcGISToSpeckleUnitConverter().ConvertOrThrow(spatialReference.Unit);
  }

  private void NormalizeAngle()
  {
    if (TrueNorthRadians < -2 * Math.PI || TrueNorthRadians > 2 * Math.PI)
    {
      TrueNorthRadians = TrueNorthRadians % 2 * Math.PI;
    }
  }

  /// <summary>
  /// Initializes a new instance of <see cref="CRSoffsetRotation"/>.
  /// </summary>
  /// <param name="spatialReference">SpatialReference to apply offsets and rotation to.</param>
  public CRSoffsetRotation(ACG.SpatialReference spatialReference)
  {
    SpatialReference = spatialReference;
    SpeckleUnitString = GetSpeckleUnit(spatialReference);
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
    ACG.SpatialReference spatialReference,
    double latOffset,
    double lonOffset,
    double trueNorthRadians
  )
  {
    SpatialReference = spatialReference;
    SpeckleUnitString = GetSpeckleUnit(spatialReference);
    LatOffset = latOffset;
    LonOffset = lonOffset;
    TrueNorthRadians = trueNorthRadians;
  }
}
