using Objects.Utils;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.Geometry.GisFeatureGeometriesToHost;

public class MeshListToHostConverter : IRawConversion<List<SOG.Mesh>, ACG.Multipatch>
{
  private readonly IRawConversion<SOG.Point, ACG.MapPoint> _pointConverter;

  public MeshListToHostConverter(IRawConversion<SOG.Point, ACG.MapPoint> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public ACG.Multipatch RawConvert(List<SOG.Mesh> target)
  {
    if (target.Count == 0)
    {
      throw new SpeckleConversionException("Feature contains no geometries");
    }
    ACG.MultipatchBuilderEx multipatchPart = new();
    foreach (SOG.Mesh part in target)
    {
      part.TriangulateMesh();
      ACG.Patch newPatch = multipatchPart.MakePatch(ACG.PatchType.Triangles);
      for (int i = 0; i < part.faces.Count; i++)
      {
        if (i % 4 == 0)
        {
          continue;
        }
        int ptIndex = part.faces[i];
        newPatch.AddPoint(
          _pointConverter.RawConvert(
            new SOG.Point(part.vertices[ptIndex * 3], part.vertices[ptIndex * 3 + 1], part.vertices[ptIndex * 3 + 2])
          )
        );
      }
      multipatchPart.Patches.Add(newPatch);
    }
    return multipatchPart.ToGeometry();
  }
}
