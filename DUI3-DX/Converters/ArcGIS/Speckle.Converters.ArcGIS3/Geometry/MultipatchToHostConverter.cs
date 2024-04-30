using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class MultipatchToHostConverter : IRawConversion<List<SGIS.GisMultipatchGeometry>, ACG.Multipatch>
{
  private readonly IRawConversion<SOG.Point, ACG.MapPoint> _pointConverter;

  public MultipatchToHostConverter(IRawConversion<SOG.Point, ACG.MapPoint> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public ACG.Multipatch RawConvert(List<SGIS.GisMultipatchGeometry> target)
  {
    ACG.MultipatchBuilderEx multipatchPart = new();
    foreach (SGIS.GisMultipatchGeometry part in target)
    {
      ACG.Patch newPatch = multipatchPart.MakePatch(ACG.PatchType.Triangles);
      for (int i = 0; i < part.vertices.Count / 3; i++)
      {
        newPatch.AddPoint(
          _pointConverter.RawConvert(
            new SOG.Point(part.vertices[i * 3], part.vertices[i * 3 + 1], part.vertices[i * 3 + 2])
          )
        );
      }
      multipatchPart.Patches.Add(newPatch);
    }
    if (multipatchPart.Patches.Count == 0)
    {
      throw new SpeckleConversionException("Conversion was not successful");
    }
    return multipatchPart.ToGeometry();
  }
}
