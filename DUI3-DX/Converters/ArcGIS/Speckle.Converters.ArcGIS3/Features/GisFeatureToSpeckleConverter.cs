using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Objects.GIS;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using Speckle.Newtonsoft.Json.Linq;
using Objects;
using System.Collections.Generic;

namespace Speckle.Converters.ArcGIS3.Features;

public class GisFeatureToSpeckleConverter : IRawConversion<Row, GisFeature>
{
  private readonly IRawConversion<ArcGIS.Core.Geometry.Geometry, IReadOnlyList<Base>> _geometryConverter;

  public GisFeatureToSpeckleConverter(
    IRawConversion<ArcGIS.Core.Geometry.Geometry, IReadOnlyList<Base>> geometryConverter
  )
  {
    _geometryConverter = geometryConverter;
  }

  public Base Convert(object target) => RawConvert((Row)target);

  public GisFeature RawConvert(Row target)
  {
    var shape = (ArcGIS.Core.Geometry.Geometry)target["Shape"];
    var speckleShapes = _geometryConverter.RawConvert(shape).ToList();
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

    // re-shape the GisFeature, if shapes is a list of Meshes
    if (speckleShapes.Count > 0 && speckleShapes[0] is SOG.Mesh)
    {
      return new GisFeature(attributes, speckleShapes);
    }
    return new GisFeature(speckleShapes, attributes);
  }
}
