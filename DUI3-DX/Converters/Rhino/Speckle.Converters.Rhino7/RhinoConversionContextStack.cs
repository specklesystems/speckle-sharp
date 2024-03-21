using System.Diagnostics.CodeAnalysis;
using Rhino;
using Speckle.Converters.Common;

namespace Speckle.Converters.Rhino7;

// POC: Suppressed naming warning for now, but we should evaluate if we should follow this or disable it.
[SuppressMessage(
  "Naming",
  "CA1711:Identifiers should not have incorrect suffix",
  Justification = "Name ends in Stack but it is in fact a Stack, just not inheriting from `System.Collections.Stack`"
)]
public class RhinoConversionContextStack : ConversionContextStack<RhinoDoc, UnitSystem>
{
  public RhinoConversionContextStack(IHostToSpeckleUnitConverter<UnitSystem> unitConverter)
    : base(RhinoDoc.ActiveDoc, RhinoDoc.ActiveDoc.ModelUnitSystem, unitConverter) { }
}
