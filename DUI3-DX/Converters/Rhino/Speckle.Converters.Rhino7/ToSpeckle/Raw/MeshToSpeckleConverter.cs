using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

[NameAndRankValue(nameof(IRhinoMesh), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class MeshToSpeckleConverter : ITypedConverter<IRhinoMesh, SOG.Mesh>
{
  private readonly ITypedConverter<IRhinoBox, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<IRhinoDoc, RhinoUnitSystem> _contextStack;
  private readonly IRhinoBoxFactory _rhinoBoxFactory;

  public MeshToSpeckleConverter(
    ITypedConverter<IRhinoBox, SOG.Box> boxConverter,
    IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack, IRhinoBoxFactory rhinoBoxFactory)
  {
    _boxConverter = boxConverter;
    _contextStack = contextStack;
    _rhinoBoxFactory = rhinoBoxFactory;
  }

  /// <summary>
  /// Converts a Rhino Mesh to a Speckle Mesh.
  /// </summary>
  /// <param name="target">The Rhino Mesh to be converted.</param>
  /// <returns>The converted Speckle Mesh.</returns>
  /// <exception cref="SpeckleConversionException">Thrown when the Rhino Mesh has 0 vertices or faces.</exception>
  public SOG.Mesh Convert(IRhinoMesh target)
  {
    if (target.Vertices.Count == 0 || target.Faces.Count == 0)
    {
      throw new SpeckleConversionException("Cannot convert a mesh with 0 vertices/faces");
    }

    var vertexCoordinates = target.Vertices.ToPoint3dArray().SelectMany(pt => new[] { pt.X, pt.Y, pt.Z }).ToList();
    var faces = new List<int>();

    foreach (IRhinoMeshNgon polygon in target.GetNgonAndFacesEnumerable())
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
    var bbox = _boxConverter.Convert(_rhinoBoxFactory.CreateBox(target.GetBoundingBox(false)));

    return new SOG.Mesh(vertexCoordinates, faces, colors, textureCoordinates, _contextStack.Current.SpeckleUnits)
    {
      volume = volume,
      bbox = bbox
    };
  }
}
