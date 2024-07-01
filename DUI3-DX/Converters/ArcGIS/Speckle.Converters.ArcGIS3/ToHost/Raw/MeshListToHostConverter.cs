using Objects.Utils;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.ToHost.Raw;

public class MeshListToHostConverter : ITypedConverter<List<SOG.Mesh>, ACG.Multipatch>
{
  private readonly ITypedConverter<SOG.Point, ACG.MapPoint> _pointConverter;
  private readonly IConversionContextStack<ArcGISDocument, ACG.Unit> _contextStack;

  public MeshListToHostConverter(
    ITypedConverter<SOG.Point, ACG.MapPoint> pointConverter,
    IConversionContextStack<ArcGISDocument, ACG.Unit> contextStack
  )
  {
    _pointConverter = pointConverter;
    _contextStack = contextStack;
  }

  public ACG.Multipatch Convert(List<SOG.Mesh> target)
  {
    if (target.Count == 0)
    {
      throw new SpeckleConversionException("Feature contains no geometries");
    }
    ACG.MultipatchBuilderEx multipatchPart = new(_contextStack.Current.Document.Map.SpatialReference);
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
          _pointConverter.Convert(
            new SOG.Point(part.vertices[ptIndex * 3], part.vertices[ptIndex * 3 + 1], part.vertices[ptIndex * 3 + 2])
            {
              units = part.units
            }
          )
        );
      }
      multipatchPart.Patches.Add(newPatch);
    }
    return multipatchPart.ToGeometry();
  }
}
