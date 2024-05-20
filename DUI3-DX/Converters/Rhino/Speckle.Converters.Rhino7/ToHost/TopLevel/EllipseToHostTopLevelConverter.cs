using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Ellipse), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class EllipseToHostTopLevelConverter : SpeckleToHostGeometryBaseConversion<SOG.Ellipse, RG.NurbsCurve>
{
  public EllipseToHostTopLevelConverter(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    ITypedConverter<SOG.Ellipse, RG.NurbsCurve> geometryBaseConverter
  )
    : base(contextStack, geometryBaseConverter) { }
}
