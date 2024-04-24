using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(DisplayableObject), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class SpeckleFallbackToHostConversion
  : ISpeckleObjectToHostConversion,
    IRawConversion<DisplayableObject, List<RG.GeometryBase>>
{
  private readonly IRawConversion<SOG.Line, RG.LineCurve> _lineConverter;
  private readonly IRawConversion<SOG.Polyline, RG.PolylineCurve> _polylineConverter;
  private readonly IRawConversion<SOG.Mesh, RG.Mesh> _meshConverter;

  public SpeckleFallbackToHostConversion(
    IRawConversion<SOG.Line, RG.LineCurve> lineConverter,
    IRawConversion<SOG.Polyline, RG.PolylineCurve> polylineConverter,
    IRawConversion<SOG.Mesh, RG.Mesh> meshConverter
  )
  {
    _lineConverter = lineConverter;
    _polylineConverter = polylineConverter;
    _meshConverter = meshConverter;
  }

  public object Convert(Base target) => RawConvert((DisplayableObject)target);

  public List<RG.GeometryBase> RawConvert(DisplayableObject target)
  {
    var result = new List<RG.GeometryBase>();
    foreach (var item in target.displayValue)
    {
      RG.GeometryBase x = item switch
      {
        SOG.Line line => _lineConverter.RawConvert(line),
        SOG.Polyline polyline => _polylineConverter.RawConvert(polyline),
        SOG.Mesh mesh => _meshConverter.RawConvert(mesh),
        _ => throw new NotSupportedException($"Found unsupported fallback geometry: {item.GetType()}")
      };
      result.Add(x);
    }

    return result;
  }
}
