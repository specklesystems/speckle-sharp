using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class HerminteSplitToSpeckleConverter : ITypedConverter<DB.HermiteSpline, SOG.Curve>
{
  private readonly ITypedConverter<DB.NurbSpline, SOG.Curve> _splineConverter;

  public HerminteSplitToSpeckleConverter(ITypedConverter<DB.NurbSpline, SOG.Curve> splineConverter)
  {
    _splineConverter = splineConverter;
  }

  public SOG.Curve RawConvert(DB.HermiteSpline target)
  {
    var nurbs = DB.NurbSpline.Create(target);
    return _splineConverter.RawConvert(nurbs);
  }
}
