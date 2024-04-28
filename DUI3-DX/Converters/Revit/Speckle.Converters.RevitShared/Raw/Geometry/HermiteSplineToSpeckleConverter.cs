using Objects;
using Objects.Primitive;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class HerminteSplitToSpeckleConverter : IRawConversion<DB.HermiteSpline, ICurve>
{
  private readonly IRawConversion<DB.NurbSpline, ICurve> _splineConverter;

  public HerminteSplitToSpeckleConverter(IRawConversion<DB.NurbSpline, ICurve> splineConverter)
  {
    _splineConverter = splineConverter;
  }

  public ICurve RawConvert(DB.HermiteSpline hermiteSpline)
  {
    var nurbs = DB.NurbSpline.Create(hermiteSpline);
    return _splineConverter.RawConvert(nurbs);
  }
}
