using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.Geometry;

[NameAndRankValue(nameof(RG.Mesh), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class MeshToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<RG.Mesh, SOG.Mesh>
{
  private readonly IRawConversion<RG.Point3d, SOG.Point> _pointConverter;
  private readonly IRawConversion<RG.Box, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public MeshToSpeckleConverter(
    IRawConversion<RG.Point3d, SOG.Point> pointConverter,
    IRawConversion<RG.Box, SOG.Box> boxConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _pointConverter = pointConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((RG.Mesh)target);

  public SOG.Mesh RawConvert(RG.Mesh target)
  {
    if (target.Vertices.Count == 0 || target.Faces.Count == 0)
    {
      throw new SpeckleConversionException("Cannot convert a mesh with 0 vertices/faces");
    }

    var vertexCoordinates = target.Vertices.ToPoint3dArray().SelectMany(pt => new[] { pt.X, pt.Y, pt.Z }).ToList();
    var faces = new List<int>();

    foreach (RG.MeshNgon polygon in target.GetNgonAndFacesEnumerable())
    {
      var vertIndices = polygon.BoundaryVertexIndexList();
      int n = vertIndices.Length;
      faces.Add(n);
      faces.AddRange(vertIndices.Select(vertIndex => (int)vertIndex));
    }

    var textureCoordinates = new List<double>(target.TextureCoordinates.Count * 2);
    foreach (var textureCoord in target.TextureCoordinates)
    {
      textureCoordinates.Add(textureCoord.X);
      textureCoordinates.Add(textureCoord.Y);
    }

    var colors = target.VertexColors.Select(cl => cl.ToArgb()).ToList();
    var volume = target.IsClosed ? target.Volume() : 0;
    var bbox = _boxConverter.RawConvert(new RG.Box(target.GetBoundingBox(true))); // POC: Why do we use accurate BBox? it's slower.

    return new SOG.Mesh(vertexCoordinates, faces, colors, textureCoordinates, _contextStack.Current.SpeckleUnits)
    {
      volume = volume,
      bbox = bbox
    };
  }
}
