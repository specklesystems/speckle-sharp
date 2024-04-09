using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.Geometry;

[NameAndRankValue(nameof(RG.Line), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class LineToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<RG.Line, SOG.Line>
{
  private readonly IRawConversion<RG.Point3d, SOG.Point> _pointConverter;
  private readonly IRawConversion<RG.Box, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public LineToSpeckleConverter(
    IRawConversion<RG.Point3d, SOG.Point> pointConverter,
    IRawConversion<RG.Box, SOG.Box> boxConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _pointConverter = pointConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((RG.Line)target);

  public SOG.Line RawConvert(RG.Line target) =>
    new(
      _pointConverter.RawConvert(target.From),
      _pointConverter.RawConvert(target.To),
      _contextStack.Current.SpeckleUnits
    )
    {
      length = target.Length,
      domain = new SOP.Interval(0, target.Length),
      bbox = _boxConverter.RawConvert(new RG.Box(target.BoundingBox))
    };
}
