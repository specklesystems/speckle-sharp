using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.GIS;
using ArcGIS.Core.Data;
using Speckle.Converters.ArcGIS3.Geometry;
using ArcGIS.Core.Data.Exceptions;
using ArcGIS.Desktop.Mapping;

namespace Speckle.Converters.ArcGIS3.Features;

public class GisFeatureToSpeckleConverter : IRawConversion<Row, GisFeature>
{
  private readonly IRawConversion<ACG.Geometry, IReadOnlyList<Base>> _geometryConverter;
  private readonly IGeometryUtils _geomUtils;

  public GisFeatureToSpeckleConverter(
    IRawConversion<ACG.Geometry, IReadOnlyList<Base>> geometryConverter,
    IGeometryUtils geomUtils
  )
  {
    _geometryConverter = geometryConverter;
    _geomUtils = geomUtils;
  }

  public Base Convert(object target) => RawConvert((Row)target);

  public GisFeature RawConvert(Row target)
  {
    // get attributes
    var attributes = new Base();
    IReadOnlyList<Field> fields = target.GetFields();
    foreach (Field field in fields)
    {
      string name = field.Name;
      if (name != "Shape") // ignore the field with geometry itself
      {
        try
        {
          var value = target[name];
          attributes[name] = value; // can be null
        }
        catch (ArgumentException)
        {
          // TODO: log in the conversion errors list
          attributes[name] = null;
        }
      }
    }

    try
    {
      var shape = (ACG.Geometry)target["Shape"];
      var speckleShapes = _geometryConverter.RawConvert(shape).ToList();

      // re-shape the GisFeature:
      // if shapes is a list of Meshes, set them as DisplayValue instead of geometry
      if (speckleShapes.Count > 0 && speckleShapes[0] is SOG.Mesh)
      {
        var displayVal = speckleShapes;
        return new GisFeature(attributes, displayVal);
      }

      // if shapes is a list of GisPolygonGeometry, create DisplayValue for those with no voids
      if (speckleShapes.Count > 0 && speckleShapes[0] is GisPolygonGeometry)
      {
        List<Base> displayVal = new();
        foreach (var poly in speckleShapes)
        {
          if (poly is GisPolygonGeometry polygon && polygon.voids.Count == 0)
          {
            // counter-clockwise orientation for up-facing mesh faces
            List<SOG.Point> boundaryPts = polygon.boundary.GetPoints();
            bool isClockwise = _geomUtils.IsClockwisePolygon(boundaryPts);
            if (isClockwise)
            {
              boundaryPts.Reverse();
            }

            // generate Mesh
            int ptCount = boundaryPts.Count;
            List<int> faces = new() { ptCount };
            faces.AddRange(Enumerable.Range(0, ptCount).ToList());

            displayVal.Add(
              new SOG.Mesh(boundaryPts.SelectMany(x => new List<double> { x.x, x.y, x.z }).ToList(), faces)
            );
          }
        }
        // POC: how to deal with multiploygons, where not all would have displayValue? Will they not be all received in other apps?
        return new GisFeature(speckleShapes, attributes, displayVal);
      }

      // otherwise set shapes as Geometries
      return new GisFeature(speckleShapes, attributes);
    }
    catch (Exception ex) when (ex is ArgumentOutOfRangeException or GeodatabaseFieldException) // if no geometry
    {
      return new GisFeature(attributes);
    }
  }
}
