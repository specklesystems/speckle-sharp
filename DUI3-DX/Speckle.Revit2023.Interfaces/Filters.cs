namespace Speckle.Revit2023.Interfaces;

#pragma warning disable CA1040
public interface IRevitFilterFactory
{
  IRevitElementIsElementTypeFilter CreateElementIsElementTypeFilter(bool inverted);
  IRevitElementMulticategoryFilter CreateElementMulticategoryFilter(ICollection<RevitBuiltInCategory> categories,bool inverted);
  IRevitLogicalAndFilterFilter CreateLogicalAndFilter(params IRevitElementFilter[] filters);
  IRevitFilteredElementCollector CreateFilteredElementCollector(IRevitDocument document, params IRevitElementId[] elementIds);
}

public interface IRevitElementFilter
{
}

public interface IRevitElementIsElementTypeFilter : IRevitElementFilter
{
  
}
public interface IRevitElementMulticategoryFilter : IRevitElementFilter
{
  
}
public interface IRevitLogicalAndFilterFilter : IRevitElementFilter
{
  
}
public interface IRevitFilteredElementCollector : IRevitElementFilter
{
  IEnumerable<T> OfClass<T>();
}
