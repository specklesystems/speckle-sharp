using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.GIS;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Threading.Tasks;

namespace Speckle.Converters.ArcGIS3.Features;

public class GisFeatureToSpeckleConverter : IRawConversion<Row, GisFeature>
{
  private readonly IRawConversion<ACG.Geometry, IReadOnlyList<Base>> _geometryConverter;

  public GisFeatureToSpeckleConverter(IRawConversion<ACG.Geometry, IReadOnlyList<Base>> geometryConverter)
  {
    _geometryConverter = geometryConverter;
  }

  public Base Convert(object target) => RawConvert((Row)target);

  public GisFeature RawConvert(Row target)
  {
    var attributes = new Base();
    QueuedTask.Run(() =>
    {
      IReadOnlyList<Field> fields = target.GetFields();
      foreach (Field field in fields)
      {
        string name = field.Name;
        // Shape is geometry itself
        if (name != "Shape")
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
    });

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
            int ptCount = polygon.boundary.GetPoints().Count;
            List<int> faces = new() { ptCount };
            faces.AddRange(Enumerable.Range(0, ptCount).ToList());
            displayVal.Add(new SOG.Mesh(polygon.boundary.value, faces));
          }
        }
        return new GisFeature(speckleShapes, attributes, displayVal);
      }
      // otherwise set shapes as Geometries
      return new GisFeature(speckleShapes, attributes);
    }
    catch (ArgumentOutOfRangeException) // if no geometry
    {
      return new GisFeature(attributes);
    }
  }
}
