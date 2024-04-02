using Autodesk.Revit.DB;
using Speckle.Converters.Common;

namespace Speckle.Converters.RevitShared.Helpers;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
  "Naming",
  "CA1711:Identifiers should not have incorrect suffix",
  Justification = "See base class justification"
)]
public class RevitConversionContextStack : ConversionContextStack<UI.UIDocument, ForgeTypeId>
{
  public const double TOLERANCE = 0.0164042; // 5mm in ft

  public RevitConversionContextStack(RevitContext context, IHostToSpeckleUnitConverter<ForgeTypeId> unitConverter)
    : base(
      context.UIApplication?.ActiveUIDocument
        ?? throw new SpeckleConversionException("Active UI document could not be determined"),
      context.UIApplication.ActiveUIDocument.Document.GetUnits().GetFormatOptions(SpecTypeId.Length).GetUnitTypeId(),
      unitConverter
    ) { }
}
