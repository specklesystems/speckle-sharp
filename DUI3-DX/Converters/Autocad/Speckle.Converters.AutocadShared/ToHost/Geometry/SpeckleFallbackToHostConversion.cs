using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(DisplayableObject), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class SpeckleFallbackToAutocadConversion
  : ISpeckleObjectToHostConversion,
    IRawConversion<DisplayableObject, List<ADB.Entity>>
{
  private readonly IRawConversion<SOG.Line, ADB.Line> _lineConverter;
  private readonly IRawConversion<SOG.Polyline, ADB.Polyline3d> _polylineConverter;
  private readonly IRawConversion<SOG.Mesh, ADB.PolyFaceMesh> _meshConverter;

  public SpeckleFallbackToAutocadConversion(
    IRawConversion<SOG.Line, ADB.Line> lineConverter,
    IRawConversion<SOG.Polyline, ADB.Polyline3d> polylineConverter,
    IRawConversion<SOG.Mesh, ADB.PolyFaceMesh> meshConverter
  )
  {
    _lineConverter = lineConverter;
    _polylineConverter = polylineConverter;
    _meshConverter = meshConverter;
  }

  public object Convert(Base target) => RawConvert((DisplayableObject)target);

  public List<ADB.Entity> RawConvert(DisplayableObject target)
  {
    var result = new List<ADB.Entity>();
    foreach (var item in target.displayValue)
    {
      ADB.Entity x = item switch
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
