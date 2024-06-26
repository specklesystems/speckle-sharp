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

  /// <summary>
  /// Initializes a new instance of <see cref="CRSoffsetRotation"/>.
  /// </summary>
  /// <param name="spatialReference">Latitude (Y) ofsset in the current SpatialReference units.</param>
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
