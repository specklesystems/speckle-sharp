using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class BoxToSpeckleConverter : ITypedConverter<IRhinoBox, SOG.Box>
{
  private readonly ITypedConverter<IRhinoPlane, SOG.Plane> _planeConverter;
  private readonly ITypedConverter<IRhinoInterval, SOP.Interval> _intervalConverter;
  private readonly IConversionContextStack<IRhinoDoc, RhinoUnitSystem> _contextStack;

  public BoxToSpeckleConverter(
    ITypedConverter<IRhinoPlane, SOG.Plane> planeConverter,
    ITypedConverter<IRhinoInterval, SOP.Interval> intervalConverter,
    IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack
  )
  {
    _planeConverter = planeConverter;
    _intervalConverter = intervalConverter;
    _contextStack = contextStack;
  }

  /// <summary>
  /// Converts a Rhino Box object to a Speckle Box object.
  /// </summary>
  /// <param name="target">The Rhino Box object to convert.</param>
  /// <returns>The converted Speckle Box object.</returns>
  public SOG.Box Convert(IRhinoBox target) =>
    new(
      _planeConverter.Convert(target.Plane),
      _intervalConverter.Convert(target.X),
      _intervalConverter.Convert(target.Y),
      _intervalConverter.Convert(target.Z),
      _contextStack.Current.SpeckleUnits
    )
    {
      area = target.Area,
      volume = target.Volume
    };
}
