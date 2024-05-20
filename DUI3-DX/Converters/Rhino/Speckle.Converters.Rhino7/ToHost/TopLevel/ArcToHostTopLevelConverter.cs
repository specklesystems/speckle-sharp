using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Arc), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class ArcToHostTopLevelConverter : SpeckleToHostGeometryBaseConversion<SOG.Arc, RG.ArcCurve>
{
  public ArcToHostTopLevelConverter(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    ITypedConverter<SOG.Arc, RG.ArcCurve> geometryBaseConverter
  )
    : base(contextStack, geometryBaseConverter) { }
}
