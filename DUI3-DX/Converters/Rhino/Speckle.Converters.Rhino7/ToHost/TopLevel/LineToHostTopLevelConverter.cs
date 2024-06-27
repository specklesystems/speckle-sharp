using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Line), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class LineToHostTopLevelConverter : SpeckleToHostGeometryBaseTopLevelConverter<SOG.Line, RG.LineCurve>
{
  public LineToHostTopLevelConverter(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    ITypedConverter<SOG.Line, RG.LineCurve> geometryBaseConverter
  )
    : base(contextStack, geometryBaseConverter) { }
}
