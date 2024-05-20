using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Polyline), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolylineToHostTopLevelConverter : SpeckleToHostGeometryBaseTopLevelConverter<SOG.Polyline, RG.PolylineCurve>
{
  public PolylineToHostTopLevelConverter(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    ITypedConverter<SOG.Polyline, RG.PolylineCurve> geometryBaseConverter
  )
    : base(contextStack, geometryBaseConverter) { }
}
