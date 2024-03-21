﻿using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.Geometry;

[NameAndRankValue(nameof(RG.Circle), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class CircleToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<RG.Circle, SOG.Circle>
{
  private readonly IRawConversion<RG.Plane, SOG.Plane> _planeConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public CircleToSpeckleConverter(
    IRawConversion<RG.Plane, SOG.Plane> planeConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _planeConverter = planeConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((RG.Circle)target);

  public SOG.Circle RawConvert(RG.Circle target) =>
    new(_planeConverter.RawConvert(target.Plane), target.Radius, _contextStack.Current.SpeckleUnits)
    {
      domain = new SOP.Interval(0, 1),
      length = 2 * Math.PI * target.Radius,
      area = Math.PI * Math.Pow(target.Radius, 2)
    };
}
