using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class BoxToSpeckleConverter : ITypedConverter<RG.Box, SOG.Box>
{
  private readonly ITypedConverter<RG.Plane, SOG.Plane> _planeConverter;
  private readonly ITypedConverter<RG.Interval, SOP.Interval> _intervalConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public BoxToSpeckleConverter(
    ITypedConverter<RG.Plane, SOG.Plane> planeConverter,
    ITypedConverter<RG.Interval, SOP.Interval> intervalConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
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
  public SOG.Box Convert(RG.Box target) =>
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
