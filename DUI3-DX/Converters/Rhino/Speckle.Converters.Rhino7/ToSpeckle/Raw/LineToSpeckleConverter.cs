using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class LineToSpeckleConverter : ITypedConverter<IRhinoLine, SOG.Line>, ITypedConverter<IRhinoLineCurve, SOG.Line>
{
  private readonly ITypedConverter<IRhinoPoint3d, SOG.Point> _pointConverter;
  private readonly ITypedConverter<IRhinoBox, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<IRhinoDoc, RhinoUnitSystem> _contextStack;
  private readonly IRhinoBoxFactory _rhinoBoxFactory;

  public LineToSpeckleConverter(
    ITypedConverter<IRhinoPoint3d, SOG.Point> pointConverter,
    ITypedConverter<IRhinoBox, SOG.Box> boxConverter,
    IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack, IRhinoBoxFactory rhinoBoxFactory)
  {
    _pointConverter = pointConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
    _rhinoBoxFactory = rhinoBoxFactory;
  }

  /// <summary>
  /// Converts a Rhino Line object to a Speckle Line object.
  /// </summary>
  /// <param name="target">The Rhino Line object to convert.</param>
  /// <returns>The converted Speckle Line object.</returns>
  /// <remarks>
  /// ⚠️ This conversion assumes the domain of a line is (0, LENGTH), as Rhino Lines do not have domain. If you want the domain preserved use LineCurve conversions instead.
  /// </remarks>
  public SOG.Line Convert(IRhinoLine target) =>
    new(_pointConverter.Convert(target.From), _pointConverter.Convert(target.To), _contextStack.Current.SpeckleUnits)
    {
      length = target.Length,
      domain = new SOP.Interval(0, target.Length),
      bbox = _boxConverter.Convert(_rhinoBoxFactory.CreateBox(target.BoundingBox))
    };

  public SOG.Line Convert(IRhinoLineCurve target) => Convert(target.Line);
}
