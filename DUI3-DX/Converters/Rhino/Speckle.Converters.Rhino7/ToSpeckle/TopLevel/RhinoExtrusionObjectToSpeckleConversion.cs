using Rhino.DocObjects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

[NameAndRankValue(nameof(ExtrusionObject), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class RhinoExtrusionObjectToSpeckleConversion : IHostObjectToSpeckleConversion
{
  private readonly IRawConversion<RG.Brep, SOG.Brep> _curveConverter;

  public RhinoExtrusionObjectToSpeckleConversion(IRawConversion<RG.Brep, SOG.Brep> curveConverter)
  {
    _curveConverter = curveConverter;
  }

  public Base Convert(object target)
  {
    var curveObject = (ExtrusionObject)target;
    var speckleCurve = _curveConverter.RawConvert(curveObject.ExtrusionGeometry.ToBrep());
    return speckleCurve;
  }
}
