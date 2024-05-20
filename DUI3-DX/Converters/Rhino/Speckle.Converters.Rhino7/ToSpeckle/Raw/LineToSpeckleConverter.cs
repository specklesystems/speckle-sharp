using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class LineToSpeckleConverter : ITypedConverter<RG.Line, SOG.Line>, ITypedConverter<RG.LineCurve, SOG.Line>
{
  private readonly ITypedConverter<RG.Point3d, SOG.Point> _pointConverter;
  private readonly ITypedConverter<RG.Box, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public LineToSpeckleConverter(
    ITypedConverter<RG.Point3d, SOG.Point> pointConverter,
    ITypedConverter<RG.Box, SOG.Box> boxConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _pointConverter = pointConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  /// <summary>
  /// Converts a Rhino Line object to a Speckle Line object.
  /// </summary>
  /// <param name="target">The Rhino Line object to convert.</param>
  /// <returns>The converted Speckle Line object.</returns>
  /// <remarks>
  /// ⚠️ This conversion assumes the domain of a line is (0, LENGTH), as Rhino Lines do not have domain. If you want the domain preserved use LineCurve conversions instead.
  /// </remarks>
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

  public SOG.Line RawConvert(RG.LineCurve target) => RawConvert(target.Line);
}
