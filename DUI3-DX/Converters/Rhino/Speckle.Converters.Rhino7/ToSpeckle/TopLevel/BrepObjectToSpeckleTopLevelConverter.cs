using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

[NameAndRankValue(nameof(IRhinoBrepObject), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class BrepObjectToSpeckleTopLevelConverter : IToSpeckleTopLevelConverter
{
  private readonly ITypedConverter<IRhinoBrep, SOG.Brep> _curveConverter;

  public BrepObjectToSpeckleTopLevelConverter(ITypedConverter<IRhinoBrep, SOG.Brep> curveConverter)
  {
    _curveConverter = curveConverter;
  }

  public Base Convert(object target)
  {
    var curveObject = (IRhinoBrepObject)target;
    var speckleCurve = _curveConverter.Convert(curveObject.BrepGeometry);
    return speckleCurve;
  }
}
