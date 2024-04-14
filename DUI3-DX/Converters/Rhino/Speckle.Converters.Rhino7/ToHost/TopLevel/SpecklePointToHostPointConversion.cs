using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

public class SpecklePointToHostPointConversion : SpeckleToHostGeometryBaseConversion<SOG.Point, RG.Point>
{
  public SpecklePointToHostPointConversion(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<SOG.Point, RG.Point> geometryBaseConverter
  )
    : base(contextStack, geometryBaseConverter) { }
}
