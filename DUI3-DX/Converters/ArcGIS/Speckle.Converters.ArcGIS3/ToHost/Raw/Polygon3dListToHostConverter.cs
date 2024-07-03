using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.ToHost.Raw;

public class Polygon3dListToHostConverter : ITypedConverter<List<SGIS.PolygonGeometry3d>, ACG.Multipatch>
{
  private readonly ITypedConverter<SOG.Point, ACG.MapPoint> _pointConverter;
  private readonly ITypedConverter<SOG.Polyline, ACG.Polyline> _polylineConverter;

  public Polygon3dListToHostConverter(
    ITypedConverter<SOG.Point, ACG.MapPoint> pointConverter,
    ITypedConverter<SOG.Polyline, ACG.Polyline> polylineConverter
  )
  {
    _pointConverter = pointConverter;
    _polylineConverter = polylineConverter;
  }

  public ACG.Multipatch Convert(List<SGIS.PolygonGeometry3d> target)
  {
    if (target.Count == 0)
    {
      throw new SpeckleConversionException("Feature contains no geometries");
    }

    ACG.MultipatchBuilderEx multipatchPart = new();
    foreach (SGIS.PolygonGeometry3d part in target)
    {
      ACG.Patch newPatch = multipatchPart.MakePatch(ACG.PatchType.FirstRing);
      List<SOG.Point> boundaryPts = part.boundary.GetPoints();
      foreach (SOG.Point pt in boundaryPts)
      {
        newPatch.AddPoint(_pointConverter.Convert(pt));
      }
      multipatchPart.Patches.Add(newPatch);

      foreach (SOG.Polyline loop in part.voids)
      {
        ACG.Patch newLoopPatch = multipatchPart.MakePatch(ACG.PatchType.Ring);
        List<SOG.Point> loopPts = loop.GetPoints();
        foreach (SOG.Point pt in loopPts)
        {
          newLoopPatch.AddPoint(_pointConverter.Convert(pt));
        }
        multipatchPart.Patches.Add(newLoopPatch);
      }
    }
    return multipatchPart.ToGeometry();
  }
}
