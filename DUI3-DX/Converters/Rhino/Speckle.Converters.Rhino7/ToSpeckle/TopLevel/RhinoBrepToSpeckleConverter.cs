using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

public sealed class RhinoBrepToSpeckleConverter : HostToSpeckleGeometryBaseConversion<RG.Brep, SOG.Brep>
{
  public RhinoBrepToSpeckleConverter(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<RG.Brep, SOG.Brep> converter
  )
    : base(contextStack, converter) { }
}
