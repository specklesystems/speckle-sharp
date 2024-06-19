using System.Diagnostics.CodeAnalysis;
using Speckle.Converters.Common;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7;

// POC: CNX-9268 Suppressed naming warning for now, but we should evaluate if we should follow this or disable it.
[SuppressMessage(
  "Naming",
  "CA1711:Identifiers should not have incorrect suffix",
  Justification = "Name ends in Stack but it is in fact a Stack, just not inheriting from `System.Collections.Stack`"
)]
public class RhinoConversionContextStack : ConversionContextStack<IRhinoDoc, RhinoUnitSystem>
{
  public RhinoConversionContextStack(IRhinoDocFactory rhinoDocFactory, IHostToSpeckleUnitConverter<RhinoUnitSystem> unitConverter)
    : base(rhinoDocFactory.ActiveDoc(), rhinoDocFactory.ActiveDoc().ModelUnitSystem, unitConverter) { }
}
