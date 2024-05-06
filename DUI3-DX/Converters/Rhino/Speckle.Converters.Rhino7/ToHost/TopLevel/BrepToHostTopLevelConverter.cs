using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Brep), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class BrepToHostTopLevelConverter : SpeckleToHostGeometryBaseConversion<SOG.Brep, RG.Brep>
{
  public BrepToHostTopLevelConverter(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<SOG.Brep, RG.Brep> geometryBaseConverter
  )
    : base(contextStack, geometryBaseConverter) { }
}
