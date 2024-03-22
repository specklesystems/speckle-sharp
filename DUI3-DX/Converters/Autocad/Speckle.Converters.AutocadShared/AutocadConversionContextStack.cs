using System.Diagnostics.CodeAnalysis;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Converters.Common;

namespace Speckle.Converters.Autocad;

// POC: Suppressed naming warning for now, but we should evaluate if we should follow this or disable it.
[SuppressMessage(
  "Naming",
  "CA1711:Identifiers should not have incorrect suffix",
  Justification = "Name ends in Stack but it is in fact a Stack, just not inheriting from `System.Collections.Stack`"
)]
public class AutocadConversionContextStack : ConversionContextStack<Document, UnitsValue>
{
  public AutocadConversionContextStack(IHostToSpeckleUnitConverter<UnitsValue> unitConverter)
    : base(
      Application.DocumentManager.CurrentDocument,
      Application.DocumentManager.CurrentDocument.Database.Insunits,
      unitConverter
    ) { }
}
