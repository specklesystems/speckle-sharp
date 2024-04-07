using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.Geometry;
using ArcGIS.Desktop.Mapping;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Geometry;

[NameAndRankValue(nameof(SOG.Polyline), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolylineToHostConverter : ISpeckleObjectToHostConversion, IRawConversion<SOG.Polyline, Polyline>
{
  private readonly IConversionContextStack<Map, Unit> _contextStack;

  public PolylineToHostConverter(IConversionContextStack<Map, Unit> contextStack)
  {
    _contextStack = contextStack;
  }

  public object Convert(Base target) => RawConvert((SOG.Polyline)target);

  public Polyline RawConvert(SOG.Polyline target)
  {
    double f = Units.GetConversionFactor(target.units, _contextStack.Current.SpeckleUnits);
    List<double> coordinates = target.value;

    // POC: retrieve here exact wkt. Not hard coded!
    string wkt =
      "PROJCS[\"WGS_1984_Web_Mercator_Auxiliary_Sphere\",GEOGCS[\"GCS_WGS_1984\",DATUM[\"D_WGS_1984\",SPHEROID[\"WGS_1984\",6378137.0,298.257223563]],PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.0174532925199433]],PROJECTION[\"Mercator_Auxiliary_Sphere\"],PARAMETER[\"False_Easting\",0.0],PARAMETER[\"False_Northing\",0.0],PARAMETER[\"Central_Meridian\",0.0],PARAMETER[\"Standard_Parallel_1\",0.0],PARAMETER[\"Auxiliary_Sphere_Type\",0.0],UNIT[\"Meter\",1.0]]\n";

    SpatialReference spatialReference = SpatialReferenceBuilder.CreateSpatialReference(wkt);

    var points = coordinates
      .Select((value, index) => new { value, index })
      .GroupBy(x => x.index / 3)
      .Select(
        group =>
          MapPointBuilder.CreateMapPoint(
            group.ElementAt(0).value * f, // X
            group.ElementAt(1).value * f, // Y
            group.ElementAt(2).value * f, // Z
            spatialReference
          )
      )
      .ToList();

    PolylineBuilder polylineBuilder = new(points.First().SpatialReference);

    var startPoint = points.First();
    for (int i = 1; i < points.Count; i++)
    {
      var lineSegment = LineBuilder.CreateLineSegment(startPoint, points[i]);
      polylineBuilder.AddSegment(lineSegment);
      startPoint = points[i];
    }

    Polyline polyline = polylineBuilder.ToGeometry();

    polylineBuilder.Dispose();
    return polyline;
  }
}
