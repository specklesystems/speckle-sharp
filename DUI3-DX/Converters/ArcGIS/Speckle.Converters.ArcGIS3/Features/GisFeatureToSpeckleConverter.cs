using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using ArcGIS.Core.Data;
using Speckle.Converters.ArcGIS3.Geometry;
using Speckle.Converters.Common;

namespace Speckle.Converters.ArcGIS3.Features;

public class GisFeatureToSpeckleConverter : IRawConversion<Row, SGIS.GisFeature>
{
  private readonly IRawConversion<ACG.Geometry, IReadOnlyList<Base>> _geometryConverter;

  public GisFeatureToSpeckleConverter(IRawConversion<ACG.Geometry, IReadOnlyList<Base>> geometryConverter)
  {
    _geometryConverter = geometryConverter;
  }

  private List<Base> GenerateFeatureDisplayValueList(List<Base> speckleShapes)
  {
    List<Base> displayVal = new();
    foreach (var shp in speckleShapes)
    {
      if (shp is SGIS.PolygonGeometry polygon) // also will be valid for Polygon3d, as it inherits from Polygon
      {
        try
        {
          SOG.Mesh displayMesh = polygon.CreateDisplayMeshForPolygon();
          displayVal.Add(displayMesh);
        }
        catch (SpeckleConversionException)
        {
          break;
        }
      }
      else if (shp is SGIS.GisMultipatchGeometry multipatch)
      {
        try
        {
          SOG.Mesh displayMesh = multipatch.CreateDisplayMeshForMultipatch();
          displayVal.Add(displayMesh);
        }
        catch (SpeckleConversionException)
        {
          break;
        }
      }
    }
    return displayVal;
  }

  public SGIS.GisFeature RawConvert(Row target)
  {
    // get attributes
    var attributes = new Base();
    bool hasGeometry = false;
    string geometryField = "Shape";
    IReadOnlyList<Field> fields = target.GetFields();
    foreach (Field field in fields)
    {
      string name = field.Name;
      // POC: check for all possible reserved Shape names
      if (field.FieldType == FieldType.Geometry) // ignore the field with geometry itself
      {
        hasGeometry = true;
        geometryField = name;
      }
      // Raster FieldType is not properly supported through API
      else if (
        field.FieldType == FieldType.Raster || field.FieldType == FieldType.Blob || field.FieldType == FieldType.XML
      )
      {
        attributes[name] = null;
      }
      // to not break serializer (DateOnly) and to simplify complex types
      else if (
        field.FieldType == FieldType.DateOnly
        || field.FieldType == FieldType.TimeOnly
        || field.FieldType == FieldType.TimestampOffset
      )
      {
        attributes[name] = target[name]?.ToString();
      }
      else
      {
        attributes[name] = target[name];
      }
    }

    // return GisFeatures that don't have geometry
    if (!hasGeometry)
    {
      return new SGIS.GisFeature(attributes);
    }
    else
    {
      var shape = (ACG.Geometry)target[geometryField];
      var speckleShapes = _geometryConverter.RawConvert(shape).ToList();

      // if geometry is primitive
      if (
        speckleShapes.Count > 0
        && speckleShapes[0] is not SGIS.PolygonGeometry
        && speckleShapes[0] is not SGIS.GisMultipatchGeometry
      )
      {
        return new SGIS.GisFeature(speckleShapes, attributes);
      }
      // if geometry is Polygon or Multipatch, add DisplayValue to the feature
      else
      {
        List<Base> displayVal = GenerateFeatureDisplayValueList(speckleShapes);
        // add display value ONLY if meshes were generates for all geometry parts
        // otherwise those without displayValue will be lost both in Viewer and in fallback Receive conversions
        if (speckleShapes.Count == displayVal.Count)
        {
          return new SGIS.GisFeature(speckleShapes, attributes, displayVal);
        }
        return new SGIS.GisFeature(speckleShapes, attributes);
      }
    }
  }
}
