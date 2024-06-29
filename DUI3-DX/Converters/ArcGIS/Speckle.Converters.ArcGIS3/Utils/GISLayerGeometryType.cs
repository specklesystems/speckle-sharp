using ArcGIS.Core.CIM;

namespace Speckle.Converters.ArcGIS3.Utils;

public static class GISLayerGeometryType
{
  public const string NONE = "None";
  public const string POINT = "Point";
  public const string POLYLINE = "Polyline";
  public const string POLYGON = "Polygon";
  public const string POLYGON3D = "Polygon3d";
  public const string MULTIPATCH = "Multipatch";
  public const string POINTCLOUD = "Pointcloud";

  public static string LayerGeometryTypeToSpeckle(esriGeometryType nativeGeometryType)
  {
    return nativeGeometryType switch
    {
      esriGeometryType.esriGeometryMultipoint => GISLayerGeometryType.POINT,
      esriGeometryType.esriGeometryPoint => GISLayerGeometryType.POINT,
      esriGeometryType.esriGeometryLine => GISLayerGeometryType.POLYLINE,
      esriGeometryType.esriGeometryPolyline => GISLayerGeometryType.POLYLINE,
      esriGeometryType.esriGeometryPolygon => GISLayerGeometryType.POLYGON,
      esriGeometryType.esriGeometryMultiPatch => GISLayerGeometryType.MULTIPATCH,
      _ => GISLayerGeometryType.NONE,
    };
  }

  public static ACG.GeometryType GetNativeLayerGeometryType(Objects.GIS.VectorLayer target)
  {
    string? originalGeomType = target.geomType != null ? target.geomType : target.nativeGeomType;
    return originalGeomType switch
    {
      GISLayerGeometryType.NONE => ACG.GeometryType.Unknown,
      GISLayerGeometryType.POINT => ACG.GeometryType.Multipoint,
      GISLayerGeometryType.POLYGON => ACG.GeometryType.Polygon,
      GISLayerGeometryType.POLYLINE => ACG.GeometryType.Polyline,
      GISLayerGeometryType.MULTIPATCH => ACG.GeometryType.Multipatch,
      GISLayerGeometryType.POLYGON3D => ACG.GeometryType.Multipatch,
      _ => throw new ArgumentOutOfRangeException(nameof(target)),
    };
  }
}
