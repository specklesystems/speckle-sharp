using System.Diagnostics.CodeAnalysis;
using Autodesk.Revit.DB;
using Speckle.Converters.Common;
using Speckle.ProxyGenerator;

namespace Speckle.Converters.RevitShared.Helpers;

[SuppressMessage(
  "Naming",
  "CA1711:Identifiers should not have incorrect suffix",
  Justification = "See base class justification"
)]
// POC: so this should *probably* be Document and NOT UI.UIDocument, the former is Conversion centric
// and the latter is more for connector
public class RevitConversionContextStack : ConversionContextStack<IRevitDocument, ForgeTypeId>, IRevitConversionContextStack
{
  public const double TOLERANCE = 0.0164042; // 5mm in ft

  public RevitConversionContextStack(RevitContext context, IHostToSpeckleUnitConverter<ForgeTypeId> unitConverter)
    : base(
      // POC: we probably should not get here without a valid document
      // so should this perpetuate or do we assume this is valid?
      // relting on the context.UIApplication?.ActiveUIDocument is not right
      // this should be some IActiveDocument I suspect?
      new DocumentProxy(context.UIApplication?.ActiveUIDocument?.Document
                         ?? throw new SpeckleConversionException("Active UI document could not be determined")),
      context.UIApplication.ActiveUIDocument.Document.GetUnits().GetFormatOptions(SpecTypeId.Length).GetUnitTypeId(),
      unitConverter
    ) { }
}

[Proxy(typeof(Document), false, ProxyClassAccessibility.Public, new [] {"PlanTopology", "PlanTopologies", "TypeOfStorage", "Equals"})]
[SuppressMessage("Naming", "CA1708:Identifiers should differ by more than case")]
[SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling")]
public partial interface IRevitDocument
{
}

public static class RevitDocumentExtensions
{
  public static IRevitFilteredElementCollector CreateFilteredElementCollector(this IRevitDocument revitDocument) => new FilteredElementCollectorProxy(new FilteredElementCollector(revitDocument._Instance));
}

[Proxy(typeof(FilteredElementCollector), false, ProxyClassAccessibility.Public, new [] {"GetEnumerator", "Equals"})]
[SuppressMessage("Naming", "CA1708:Identifiers should differ by more than case")]
[SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling")]
public partial interface IRevitFilteredElementCollector
{
}

public static class RevitFilteredExtensions
{
  public static IEnumerable<T> Cast<T>(this IRevitFilteredElementCollector revitFilteredElementCollector)
    => revitFilteredElementCollector._Instance.ToElements().Cast<T>();
}

[Proxy(typeof(Element), false, ProxyClassAccessibility.Public, new [] {"Equals", "Parameter", "BoundingBox", "Geometry"})]
[SuppressMessage("Naming", "CA1708:Identifiers should differ by more than case")]
[SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling")]
public partial interface IRevitElement
{
}
