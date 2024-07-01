using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Polycurve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolycurveToHostConverter : IToHostTopLevelConverter, ITypedConverter<SOG.Polycurve, ACG.Polyline>
{
  private readonly ITypedConverter<SOG.Point, ACG.MapPoint> _pointConverter;
  private readonly IRootToHostConverter _converter;
  private readonly IConversionContextStack<ArcGISDocument, ACG.Unit> _contextStack;

  public PolycurveToHostConverter(
    ITypedConverter<SOG.Point, ACG.MapPoint> pointConverter,
    IRootToHostConverter converter,
    IConversionContextStack<ArcGISDocument, ACG.Unit> contextStack
  )
  {
    _pointConverter = pointConverter;
    _converter = converter;
    _contextStack = contextStack;
  }

  public object Convert(Base target) => Convert((SOG.Polycurve)target);

  public ACG.Polyline Convert(SOG.Polycurve target)
  {
    ACG.MapPoint? lastConvertedPt = null;
    List<ACG.Polyline> segments = new();

    foreach (var segment in target.segments)
    {
      ACG.Polyline converted = (ACG.Polyline)_converter.Convert((Base)segment); //CurveConverter.NotNull().Convert(segment);
      List<ACG.MapPoint> segmentPts = converted.Points.ToList();

      if (lastConvertedPt != null && segmentPts.Count > 0 && lastConvertedPt != segmentPts[0])
      {
        throw new SpeckleConversionException("Polycurve segments are not in a correct sequence/orientation");
      }

      lastConvertedPt = segmentPts[^1];
      segments.Add(converted);
    }

    return new ACG.PolylineBuilderEx(
      segments,
      ACG.AttributeFlags.HasZ,
      _contextStack.Current.Document.Map.SpatialReference
    ).ToGeometry();
  }
}
