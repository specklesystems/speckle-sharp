﻿using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Polyline), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class SpecklePolylineToHostPolylineCurveConversion
  : SpeckleToHostGeometryBaseConversion<SOG.Polyline, RG.PolylineCurve>
{
  public SpecklePolylineToHostPolylineCurveConversion(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<SOG.Polyline, RG.PolylineCurve> geometryBaseConverter
  )
    : base(contextStack, geometryBaseConverter) { }
}
