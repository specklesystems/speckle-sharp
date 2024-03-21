using Autodesk.Revit.DB;
using Speckle.Converters.Common;

namespace Speckle.Converters.RevitShared.Helpers;

public class RevitConversionContextStack : ConversionContextStack<UI.UIDocument, ForgeTypeId>
{
  public RevitConversionContextStack(RevitContext context, IHostToSpeckleUnitConverter<ForgeTypeId> unitConverter)
    : base(
      context.UIApplication.ActiveUIDocument,
      context.UIApplication.ActiveUIDocument.Document.GetUnits().GetFormatOptions(SpecTypeId.Length).GetUnitTypeId(),
      unitConverter
    ) { }
}
