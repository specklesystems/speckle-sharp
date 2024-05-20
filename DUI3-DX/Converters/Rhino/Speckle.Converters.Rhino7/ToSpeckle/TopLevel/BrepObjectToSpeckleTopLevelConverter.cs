using Rhino.DocObjects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

[NameAndRankValue(nameof(BrepObject), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class BrepObjectToSpeckleTopLevelConverter : IToSpeckleTopLevelConverter
{
  private readonly ITypedConverter<RG.Brep, SOG.Brep> _curveConverter;

  public BrepObjectToSpeckleTopLevelConverter(ITypedConverter<RG.Brep, SOG.Brep> curveConverter)
  {
    _curveConverter = curveConverter;
  }

  public Base Convert(object target)
  {
    var curveObject = (BrepObject)target;
    var speckleCurve = _curveConverter.Convert(curveObject.BrepGeometry);
    return speckleCurve;
  }
}
