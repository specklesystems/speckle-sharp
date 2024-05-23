using System.Diagnostics.CodeAnalysis;
using Autodesk.Revit.DB;
using Speckle.ProxyGenerator;

namespace Speckle.Revit2023.Api;

[Proxy(typeof(Document), new [] {"Autodesk.Revit.DB", "PlanTopologies", "TypeOfStorage", "Equals"})]
[SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling")]

public partial interface IRevitDocument 
{
}

public static class RevitDocumentExtensions
{
  public static IRevitFilteredElementCollector CreateFilteredElementCollector(this IRevitDocument revitDocument) => new FilteredElementCollectorProxy(new FilteredElementCollector(revitDocument._Instance));
}

[Proxy(typeof(FilteredElementCollector), new [] {"Autodesk.Revit.DB","GetEnumerator", "Equals"})]
public partial interface IRevitFilteredElementCollector
{
}

public static class RevitFilteredExtensions
{
  public static IEnumerable<T> Cast<T>(this IRevitFilteredElementCollector revitFilteredElementCollector)
    => Enumerable.Cast<T>(revitFilteredElementCollector._Instance.ToElements());
}
