using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class SpeckleLineRawToHostConversion : IRawConversion<SOG.Line, RG.LineCurve>, IRawConversion<SOG.Line, RG.Line>
{
  private readonly IRawConversion<SOG.Point, RG.Point3d> _pointConverter;

  public SpeckleLineRawToHostConversion(IRawConversion<SOG.Point, RG.Point3d> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public RG.Line RawConvert(SOG.Line target) =>
    new(_pointConverter.RawConvert(target.start), _pointConverter.RawConvert(target.end));

  RG.LineCurve IRawConversion<SOG.Line, RG.LineCurve>.RawConvert(SOG.Line target) => new(RawConvert(target));
}
