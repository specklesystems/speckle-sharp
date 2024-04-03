using Autodesk.Revit.DB;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared.ToSpeckle;

[NameAndRankValue(nameof(DB.Location), 0)]
public class LocationConversionToSpeckle : BaseConversionToSpeckle<DB.Location, Base>
{
  private readonly IRawConversion<DB.Curve> _curveConverter;
  private readonly IRawConversion<DB.XYZ, SOG.Point> _xyzConverter;

  // POC: review IRawConversion<TIn> which always returns a Base, this is ToSpeckle, so... this breaks
  // the meaning of IRawConversion, it could be IToSpeckleRawConversion
  // also a factory type
  public LocationConversionToSpeckle(IRawConversion<Curve> curveConverter, IRawConversion<XYZ, SOG.Point> xyzConverter)
  {
    _curveConverter = curveConverter;
    _xyzConverter = xyzConverter;
  }

  public override Base RawConvert(DB.Location target)
  {
    return target switch
    {
      LocationCurve curve => _curveConverter.ConvertToBase(curve.Curve),
      LocationPoint point => _xyzConverter.RawConvert(point.Point),
      _ => throw new SpeckleConversionException($"Unexpected location type {target.GetType()}")
    };
  }
}
