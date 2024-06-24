namespace Speckle.Converters.ArcGIS3.Utils;

/// <summary>
/// Container with origin offsets and rotation angle
/// </summary>
public readonly struct CRSoffsetRotation
{
  public double LatOffset { get; }
  public double LonOffset { get; }
  public double TrueNorthRadians { get; }

  /// <summary>
  /// Initializes a new instance of <see cref="CRSoffsetRotation"/>.
  /// </summary>
  /// <param name="latOffset">Latitude (Y) ofsset in the current SpatialReference units.</param>
  /// <param name="lonOffset">Longitude (X) ofsset in the current SpatialReference units.</param>
  /// <param name="trueNorthRadians">Angle to True North in radians.</param>
  public CRSoffsetRotation(double latOffset, double lonOffset, double trueNorthRadians)
  {
    LatOffset = latOffset;
    LonOffset = lonOffset;
    TrueNorthRadians = trueNorthRadians;
  }
}
