using Objects.Utils;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Geometry.ISpeckleObjectToHost;

[NameAndRankValue(nameof(SOG.Mesh), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class MeshToHostConverter : ISpeckleObjectToHostConversion, IRawConversion<SOG.Mesh, ACG.Multipatch>
{
  private readonly IRawConversion<SOG.Point, ACG.MapPoint> _pointConverter;

  public MeshToHostConverter(IRawConversion<SOG.Point, ACG.MapPoint> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public object Convert(Base target) => RawConvert((SOG.Mesh)target);

  public ACG.Multipatch RawConvert(SOG.Mesh target)
  {
    target.TriangulateMesh();
    ACG.MultipatchBuilderEx multipatchPart = new();
    ACG.Patch newPatch = multipatchPart.MakePatch(ACG.PatchType.Triangles);
    for (int i = 0; i < target.VerticesCount; i++)
    {
      newPatch.AddPoint(
        _pointConverter.RawConvert(
          new SOG.Point(target.vertices[i * 3], target.vertices[i * 3 + 1], target.vertices[i * 3 + 2])
        )
      );
    }
    multipatchPart.Patches.Add(newPatch);

    return multipatchPart.ToGeometry();
  }
}
