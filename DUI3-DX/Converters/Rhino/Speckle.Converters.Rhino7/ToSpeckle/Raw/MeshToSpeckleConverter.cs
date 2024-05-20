using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

[NameAndRankValue(nameof(RG.Mesh), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class MeshToSpeckleConverter : ITypedConverter<RG.Mesh, SOG.Mesh>
{
  private readonly ITypedConverter<RG.Point3d, SOG.Point> _pointConverter;
  private readonly ITypedConverter<RG.Box, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public MeshToSpeckleConverter(
    ITypedConverter<RG.Point3d, SOG.Point> pointConverter,
    ITypedConverter<RG.Box, SOG.Box> boxConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _pointConverter = pointConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  /// <summary>
  /// Converts a Rhino Mesh to a Speckle Mesh.
  /// </summary>
  /// <param name="target">The Rhino Mesh to be converted.</param>
  /// <returns>The converted Speckle Mesh.</returns>
  /// <exception cref="SpeckleConversionException">Thrown when the Rhino Mesh has 0 vertices or faces.</exception>
  public SOG.Mesh Convert(RG.Mesh target)
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
    var bbox = _boxConverter.Convert(new RG.Box(target.GetBoundingBox(false)));

    return new SOG.Mesh(vertexCoordinates, faces, colors, textureCoordinates, _contextStack.Current.SpeckleUnits)
    {
      volume = volume,
      bbox = bbox
    };
  }
}
