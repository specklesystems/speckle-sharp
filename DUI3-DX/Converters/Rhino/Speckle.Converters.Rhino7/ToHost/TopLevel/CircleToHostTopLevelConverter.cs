﻿using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Circle), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class CircleToHostTopLevelConverter : SpeckleToHostGeometryBaseConversion<SOG.Circle, RG.ArcCurve>
{
  public CircleToHostTopLevelConverter(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<SOG.Circle, RG.ArcCurve> geometryBaseConverter
  )
    : base(contextStack, geometryBaseConverter) { }
}