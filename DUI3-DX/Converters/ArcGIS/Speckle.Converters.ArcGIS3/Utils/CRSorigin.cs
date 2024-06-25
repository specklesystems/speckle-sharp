using ArcGIS.Core.Geometry;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Utils;

/// <summary>
/// Container with origin coordinates and rotation angle
/// </summary>
public readonly struct CRSorigin
{
  public double LatDegrees { get; }
  public double LonDegrees { get; }

  /// <summary>
  /// Initializes a new instance of <see cref="CRSorigin"/>.
  /// </summary>
  /// <param name="latDegrees">Latitude (Y) in degrees.</param>
  /// <param name="lonDegrees">Longitude (X) in degrees.</param>
  public CRSorigin(double latDegrees, double lonDegrees)
  {
    LatDegrees = latDegrees;
    LonDegrees = lonDegrees;
  }

  public static CRSorigin? FromRevitData(Base rootObject)
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
            double lat = Convert.ToDouble(revitProjInfo["latitude"]);
            double lon = Convert.ToDouble(revitProjInfo["longitude"]);
            double trueNorth;
            if (revitProjInfo["locations"] is List<Base> locationList && locationList.Count > 0)
            {
              Base location = locationList[0];
              trueNorth = Convert.ToDouble(location["trueNorth"]);
            }
            return new CRSorigin(lat * 180 / Math.PI, lon * 180 / Math.PI);
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

  public SpatialReference CreateCustomCRS()
  {
    string wktString =
      $"PROJCS[\"unknown\", GEOGCS[\"unknown\", DATUM[\"WGS_1984\", SPHEROID[\"WGS 84\", 6378137, 298.257223563], AUTHORITY[\"EPSG\", \"6326\"]], PRIMEM[\"Greenwich\", 0, AUTHORITY[\"EPSG\", \"8901\"]], UNIT[\"degree\", 0.0174532925199433]], PROJECTION[\"Transverse_Mercator\"], PARAMETER[\"latitude_of_origin\", {LatDegrees}], PARAMETER[\"central_meridian\", {LonDegrees}], PARAMETER[\"scale_factor\", 1], PARAMETER[\"false_easting\", 0], PARAMETER[\"false_northing\", 0], UNIT[\"metre\", 1, AUTHORITY[\"EPSG\", \"9001\"]], AXIS[\"Easting\", EAST], AXIS[\"Northing\", NORTH]]";
    return SpatialReferenceBuilder.CreateSpatialReference(wktString);
  }
}
