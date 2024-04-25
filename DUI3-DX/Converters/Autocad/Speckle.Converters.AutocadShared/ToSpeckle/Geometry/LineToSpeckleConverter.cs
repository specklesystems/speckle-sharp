using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToSpeckle.Geometry;

[NameAndRankValue(nameof(ADB.Line), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class LineToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<ADB.Line, SOG.Line>
{
  private readonly IRawConversion<AG.Point3d, SOG.Point> _pointConverter;
  private readonly IRawConversion<ADB.Extents3d, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public LineToSpeckleConverter(
    IRawConversion<AG.Point3d, SOG.Point> pointConverter,
    IRawConversion<ADB.Extents3d, SOG.Box> boxConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _pointConverter = pointConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((ADB.Line)target);

  public SOG.Line RawConvert(ADB.Line target) =>
    new(
      _pointConverter.RawConvert(target.StartPoint),
      _pointConverter.RawConvert(target.EndPoint),
      _contextStack.Current.SpeckleUnits
    )
    {
      length = target.Length,
      domain = new SOP.Interval(0, target.Length),
      bbox = _boxConverter.RawConvert(target.GeometricExtents)
    };
}
