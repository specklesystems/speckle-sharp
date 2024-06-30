using Objects.BuiltElements.Revit;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Utils;

/// <summary>
/// Container with origin offsets and rotation angle for the specified SpatialReference
/// Offsets and rotation will modify geometry on Send, so non-GIS apps can receive it correctly
/// Receiving GIS geometry in GIS hostApp will "undo" the geometry modifications according to the offsets&rotation applied before
/// In the future, CAD/BIM objects will contain ProjectInfo data with CRS & offsets, so this object can be generated on Recieve
/// TODO: consider how to generate this object to receive non-GIS data already now, without it having ProjectInfo object
/// </summary>
public struct CRSoffsetRotation
{
  public ACG.SpatialReference SpatialReference { get; }
  public string SpeckleUnitString { get; set; }
  public double LatOffset { get; set; }
  public double LonOffset { get; set; }
  public double TrueNorthRadians { get; set; }

  public SOG.Point OffsetRotateOnReceive(SOG.Point pointOriginal)
  {
    // scale point to match units of the SpatialReference
    string originalUnits = pointOriginal.units;
    SOG.Point point = ScalePoint(pointOriginal, originalUnits, SpeckleUnitString);

    // 1. rotate coordinates
    NormalizeAngle();
    double x2 = point.x * Math.Cos(TrueNorthRadians) - point.y * Math.Sin(TrueNorthRadians);
    double y2 = point.x * Math.Sin(TrueNorthRadians) + point.y * Math.Cos(TrueNorthRadians);
    // 2. offset coordinates
    x2 += LonOffset;
    y2 += LatOffset;
    SOG.Point movedPoint = new(x2, y2, point.z, SpeckleUnitString);

    return movedPoint;
  }

  public SOG.Point OffsetRotateOnSend(SOG.Point point)
  {
    // scale point to match units of the SpatialReference
    string originalUnits = point.units;
    point = ScalePoint(point, originalUnits, SpeckleUnitString);

    // 1. offset coordinates
    NormalizeAngle();
    double x2 = point.x - LonOffset;
    double y2 = point.y - LatOffset;
    // 2. rotate coordinates
    x2 = x2 * Math.Cos(TrueNorthRadians) + y2 * Math.Sin(TrueNorthRadians);
    y2 = -x2 * Math.Sin(TrueNorthRadians) + y2 * Math.Cos(TrueNorthRadians);
    SOG.Point movedPoint = new(x2, y2, point.z, SpeckleUnitString);

    return movedPoint;
  }

  private readonly SOG.Point ScalePoint(SOG.Point point, string fromUnit, string toUnit)
  {
    double scaleFactor = Units.GetConversionFactor(fromUnit, toUnit);
    return new SOG.Point(point.x * scaleFactor, point.y * scaleFactor, point.z * scaleFactor, toUnit);
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

  public static double? RotationFromRevitData(Base rootObject)
  {
    // rewrite function to take into account Local reference point in Revit, and Transformation matrix
    foreach (KeyValuePair<string, object?> prop in rootObject.GetMembers(DynamicBaseMemberType.Dynamic))
    {
      if (prop.Key == "info")
      {
        ProjectInfo? revitProjInfo = (ProjectInfo?)rootObject[prop.Key];
        if (revitProjInfo != null)
        {
          try
          {
            if (revitProjInfo["locations"] is List<Base> locationList && locationList.Count > 0)
            {
              Base location = locationList[0];
              return Convert.ToDouble(location["trueNorth"]);
            }
          }
          catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException)
          {
            // origin not found, do nothing
          }
          break;
        }
      }
    }
    return null;
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
  /// <param name="trueNorthRadians">Angle to True North in radians.</param>
  public CRSoffsetRotation(ACG.SpatialReference spatialReference, double trueNorthRadians)
  {
    SpatialReference = spatialReference;
    SpeckleUnitString = GetSpeckleUnit(spatialReference);
    LatOffset = 0;
    LonOffset = 0;
    TrueNorthRadians = trueNorthRadians;
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
