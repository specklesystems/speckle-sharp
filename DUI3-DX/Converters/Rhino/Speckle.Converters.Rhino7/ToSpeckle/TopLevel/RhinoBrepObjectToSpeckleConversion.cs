using Rhino.DocObjects;
using Rhino.Runtime;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

[NameAndRankValue(nameof(BrepObject), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class RhinoBrepObjectToSpeckleConversion : IHostObjectToSpeckleConversion<CommonObject>
{
  private readonly IRawConversion<RG.Brep, SOG.Brep> _curveConverter;

  public RhinoBrepObjectToSpeckleConversion(IRawConversion<RG.Brep, SOG.Brep> curveConverter)
  {
    _curveConverter = curveConverter;
  }

  public Base Convert(CommonObject target)
  {
    var curveObject = (BrepObject)target;
    var speckleCurve = _curveConverter.RawConvert(curveObject.BrepGeometry);
    return speckleCurve;
  }
}
