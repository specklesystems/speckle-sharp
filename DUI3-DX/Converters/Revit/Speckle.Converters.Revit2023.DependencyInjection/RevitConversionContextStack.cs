using Autodesk.Revit.DB;
using Speckle.Converters.Common;
using Speckle.Converters.RevitShared;
using Speckle.Revit.Api;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.DependencyInjection;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
  "Naming",
  "CA1711:Identifiers should not have incorrect suffix",
  Justification = "See base class justification"
)]
// POC: so this should *probably* be Document and NOT UI.UIDocument, the former is Conversion centric
// and the latter is more for connector
public class RevitConversionContextStack
  : ConversionContextStack<IRevitDocument, IRevitForgeTypeId>,
    IConversionContextStack<IRevitDocument, IRevitForgeTypeId>
{
  public const double TOLERANCE = RevitConstants.TOLERANCE;

  public RevitConversionContextStack(RevitContext context, IHostToSpeckleUnitConverter<IRevitForgeTypeId> unitConverter)
    : base(
      // POC: we probably should not get here without a valid document
      // so should this perpetuate or do we assume this is valid?
      // relting on the context.UIApplication?.ActiveUIDocument is not right
      // this should be some IActiveDocument I suspect?
      new DocumentProxy(
        context.UIApplication?.ActiveUIDocument?.Document
          ?? throw new SpeckleConversionException("Active UI document could not be determined")
      ),
      new ForgeTypeIdProxy(
        context.UIApplication.ActiveUIDocument.Document.GetUnits().GetFormatOptions(SpecTypeId.Length).GetUnitTypeId()
      ),
      unitConverter
    ) { }

  IConversionContext<IRevitDocument> IConversionContextStack<IRevitDocument, IRevitForgeTypeId>.Current =>
    new ConversionContext<IRevitDocument>(base.Current.Document, base.Current.SpeckleUnits);

  ContextWrapper<IRevitDocument, IRevitForgeTypeId> IConversionContextStack<IRevitDocument, IRevitForgeTypeId>.Push(
    string speckleUnit
  ) => throw new NotImplementedException();
}
