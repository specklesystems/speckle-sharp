﻿using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class ArcToSpeckleConverter : IRawConversion<RG.Arc, SOG.Arc>
{
  private readonly IRawConversion<RG.Point3d, SOG.Point> _pointConverter;
  private readonly IRawConversion<RG.Plane, SOG.Plane> _planeConverter;
  private readonly IRawConversion<RG.Box, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public ArcToSpeckleConverter(
    IRawConversion<RG.Point3d, SOG.Point> pointConverter,
    IRawConversion<RG.Plane, SOG.Plane> planeConverter,
    IRawConversion<RG.Box, SOG.Box> boxConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _pointConverter = pointConverter;
    _planeConverter = planeConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  /// <summary>
  /// Converts a Rhino Arc object to a Speckle Arc object.
  /// </summary>
  /// <param name="target">The Rhino Arc object to convert.</param>
  /// <returns>The converted Speckle Arc object.</returns>
  /// <remarks>
  /// This method assumes the domain of the arc is (0,1) as Arc types in Rhino do not have domain. You may want to request a conversion from ArcCurve instead.
  /// </remarks>
  public SOG.Arc RawConvert(RG.Arc target) =>
    new(
      _planeConverter.RawConvert(target.Plane),
      target.Radius,
      target.StartAngle,
      target.EndAngle,
      target.Angle,
      _contextStack.Current.SpeckleUnits
    )
    {
      startPoint = _pointConverter.RawConvert(target.StartPoint),
      midPoint = _pointConverter.RawConvert(target.MidPoint),
      endPoint = _pointConverter.RawConvert(target.EndPoint),
      domain = new SOP.Interval(0, 1),
      length = target.Length,
      bbox = _boxConverter.RawConvert(new RG.Box(target.BoundingBox()))
    };
}
