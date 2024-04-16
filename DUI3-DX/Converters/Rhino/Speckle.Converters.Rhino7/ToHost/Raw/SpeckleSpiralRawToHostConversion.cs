using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class SpeckleSpiralRawToHostConversion : IRawConversion<SOG.Spiral, RG.PolylineCurve>
{
  private readonly IRawConversion<SOG.Polyline, RG.PolylineCurve> _polylineConverter;

  public SpeckleSpiralRawToHostConversion(IRawConversion<SOG.Polyline, RG.PolylineCurve> polylineConverter)
  {
    _polylineConverter = polylineConverter;
  }

  public RG.PolylineCurve RawConvert(SOG.Spiral target) => _polylineConverter.RawConvert(target.displayValue);
}
