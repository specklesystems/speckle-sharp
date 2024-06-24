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
  /// Initializes a new instance of <see cref="CRSorigin"/>.
  /// </summary>
  /// <param name="latDegrees">Latitude (Y) in degrees.</param>
  /// <param name="lonDegrees">Longitude (X) in degrees.</param>
  /// <param name="trueNorthRadians">Angle to True North in radians.</param>
  public CRSoffsetRotation(double latDegrees, double lonDegrees, double trueNorthRadians)
  {
    LatOffset = latDegrees;
    LonOffset = lonDegrees;
    TrueNorthRadians = trueNorthRadians;
  }
}
