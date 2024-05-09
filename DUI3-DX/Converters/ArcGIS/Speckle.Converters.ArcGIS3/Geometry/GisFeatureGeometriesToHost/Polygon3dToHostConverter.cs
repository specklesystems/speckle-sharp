using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;

namespace Speckle.Converters.ArcGIS3.Geometry.GisFeatureGeometriesToHost;

public class Polygon3dToHostConverter : IRawConversion<List<SGIS.PolygonGeometry3d>, ACG.Multipatch>
{
  private readonly IRawConversion<SOG.Point, ACG.MapPoint> _pointConverter;
  private readonly IRawConversion<SOG.Polyline, ACG.Polyline> _polylineConverter;

  public Polygon3dToHostConverter(
    IRawConversion<SOG.Point, ACG.MapPoint> pointConverter,
    IRawConversion<SOG.Polyline, ACG.Polyline> polylineConverter
  )
  {
    _pointConverter = pointConverter;
    _polylineConverter = polylineConverter;
  }

  public ACG.Multipatch RawConvert(List<SGIS.PolygonGeometry3d> target)
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
        newPatch.AddPoint(_pointConverter.RawConvert(pt));
      }
      multipatchPart.Patches.Add(newPatch);

      foreach (SOG.Polyline loop in part.voids)
      {
        ACG.Patch newLoopPatch = multipatchPart.MakePatch(ACG.PatchType.Ring);
        List<SOG.Point> loopPts = loop.GetPoints();
        foreach (SOG.Point pt in loopPts)
        {
          newLoopPatch.AddPoint(_pointConverter.RawConvert(pt));
        }
        multipatchPart.Patches.Add(newLoopPatch);
      }
    }
    return multipatchPart.ToGeometry();
  }
}
