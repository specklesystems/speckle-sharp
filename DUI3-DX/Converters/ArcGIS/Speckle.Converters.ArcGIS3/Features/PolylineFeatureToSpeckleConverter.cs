using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.Features;

public class PolyineFeatureToSpeckleConverter : ITypedConverter<ACG.Polyline, IReadOnlyList<SOG.Polyline>>
{
  private readonly ITypedConverter<ACG.ReadOnlySegmentCollection, SOG.Polyline> _segmentConverter;
  private readonly IConversionContextStack<ArcGISDocument, ACG.Unit> _contextStack;

  public PolyineFeatureToSpeckleConverter(
    ITypedConverter<ACG.ReadOnlySegmentCollection, SOG.Polyline> segmentConverter,
    IConversionContextStack<ArcGISDocument, ACG.Unit> contextStack
  )
  {
    _segmentConverter = segmentConverter;
    _contextStack = contextStack;
  }

  public IReadOnlyList<SOG.Polyline> Convert(ACG.Polyline target)
  {
    // https://pro.arcgis.com/en/pro-app/latest/sdk/api-reference/topic8480.html
    List<SOG.Polyline> polylineList = new();
    ACG.Polyline polylineToConvert = target;

    // segmentize the polylines with curves using precision value of the Map's Spatial Reference
    if (target.HasCurves is true)
    {
      double tolerance = _contextStack.Current.Document.Map.SpatialReference.XYTolerance;
      double conversionFactorToMeter = _contextStack.Current.Document.Map.SpatialReference.Unit.ConversionFactor;
      var densifiedPolyline = ACG.GeometryEngine.Instance.DensifyByDeviation(
        target,
        tolerance * conversionFactorToMeter
      );
      polylineToConvert = (ACG.Polyline)densifiedPolyline;
      if (densifiedPolyline == null)
      {
        throw new ArgumentException("Polyline densification failed");
      }
    }

    foreach (var segmentCollection in polylineToConvert.Parts)
    {
      polylineList.Add(_segmentConverter.Convert(segmentCollection));
    }
    return polylineList;
  }
}
