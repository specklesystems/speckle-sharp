using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToSpeckle.Raw;

public class Solid3dToSpeckleRawConverter : ITypedConverter<ADB.Solid3d, SOG.Mesh>
{
  private readonly ITypedConverter<AG.Point3d, SOG.Point> _pointConverter;
  private readonly ITypedConverter<ADB.Extents3d, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public Solid3dToSpeckleRawConverter(
    ITypedConverter<AG.Point3d, SOG.Point> pointConverter,
    ITypedConverter<ADB.Extents3d, SOG.Box> boxConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _pointConverter = pointConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => Convert((ADB.Solid3d)target);

  public SOG.Mesh Convert(ADB.Solid3d target)
  {
    using ABR.Brep brep = new(target);
    if (brep.IsNull)
    {
      throw new SpeckleConversionException("Could not retrieve brep from the solid3d.");
    }

    var vertices = new List<AG.Point3d>();
    var faces = new List<int>();

    // create mesh from solid with mesh filter
    using ABR.Mesh2dControl control = new();
    control.MaxSubdivisions = 10000; // POC: these settings may need adjusting
    using ABR.Mesh2dFilter filter = new();
    filter.Insert(brep, control);
    using ABR.Mesh2d m = new(filter);
    foreach (ABR.Element2d e in m.Element2ds)
    {
      // get vertices
      List<int> faceIndices = new();
      foreach (ABR.Node n in e.Nodes)
      {
        faceIndices.Add(vertices.Count);
        vertices.Add(n.Point);
        n.Dispose();
      }

      // get faces
      List<int> faceList = new() { e.Nodes.Count() };
      for (int i = 0; i < e.Nodes.Count(); i++)
      {
        faceList.Add(faceIndices[i]);
      }

      faces.AddRange(faceList);

      e.Dispose();
    }

    // mesh props
    var convertedVertices = vertices.SelectMany(o => _pointConverter.Convert(o).ToList()).ToList();
    double volume = target.MassProperties.Volume;
    double area = target.Area;
    SOG.Box bbox = _boxConverter.Convert(target.GeometricExtents);

    // create speckle mesh
    SOG.Mesh mesh =
      new(convertedVertices, faces)
      {
        units = _contextStack.Current.SpeckleUnits,
        bbox = bbox,
        area = area,
        volume = volume
      };

    return mesh;
  }
}
