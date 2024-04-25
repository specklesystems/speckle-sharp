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

  public Base Convert(object target) => RawConvert((Row)target);

  public SGIS.GisFeature RawConvert(Row target)
  {
    // get attributes
    var attributes = new Base();
    bool hasGeometry = false;
    IReadOnlyList<Field> fields = target.GetFields();
    foreach (Field field in fields)
    {
      string name = field.Name;
      if (name != "Shape") // ignore the field with geometry itself
      {
        try
        {
          object? value = target[name];
          attributes[name] = value;
        }
        catch (ArgumentException)
        {
          // TODO: log in the conversion errors list
          attributes[name] = null;
        }
      }
      else
      {
        hasGeometry = true;
      }
    }

    // return GisFeatures that don't have geometry
    if (!hasGeometry)
    {
      return new SGIS.GisFeature(attributes);
    }
    else
    {
      var shape = (ACG.Geometry)target["Shape"];
      var speckleShapes = _geometryConverter.RawConvert(shape).ToList();

      // if geometry is primitive
      if (
        speckleShapes.Count > 0
        && speckleShapes[0] is not SGIS.GisPolygonGeometry
        && speckleShapes[0] is not SGIS.GisMultipatchGeometry
      )
      {
        return new SGIS.GisFeature(speckleShapes, attributes);
      }
      // if geometry is Polygon or Multipatch, add DisplayValue to the feature
      else
      {
        List<Base> displayVal = new();
        foreach (var shp in speckleShapes)
        {
          if (shp is SGIS.GisPolygonGeometry polygon)
          {
            try
            {
              SOG.Mesh displayMesh = polygon.CreateDisplayMeshForPolygon();
              displayVal.Add(displayMesh);
            }
            catch (SpeckleConversionException)
            {
              continue;
            }
          }
          else if (shp is SGIS.GisPolygonGeometry3d polygon3d)
          {
            try
            {
              SOG.Mesh displayMesh = polygon3d.CreateDisplayMeshForPolygon3d();
              displayVal.Add(displayMesh);
            }
            catch (SpeckleConversionException)
            {
              continue;
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
              continue;
            }
          }
        }
        // add display value ONLY if meshes were generates for each geometry part
        if (speckleShapes.Count == displayVal.Count)
        {
          return new SGIS.GisFeature(speckleShapes, attributes, displayVal);
        }
        return new SGIS.GisFeature(speckleShapes, attributes);
      }
    }
  }
}
