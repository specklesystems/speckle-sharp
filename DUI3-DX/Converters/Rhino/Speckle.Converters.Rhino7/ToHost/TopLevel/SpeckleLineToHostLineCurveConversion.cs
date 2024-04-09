using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

public class SpeckleLineToHostLineCurveConversion : SpeckleToHostGeometryBaseConversion<SOG.Line, RG.LineCurve>
{
  public SpeckleLineToHostLineCurveConversion(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<SOG.Line, RG.LineCurve> geometryBaseConverter
  )
    : base(contextStack, geometryBaseConverter) { }
}
