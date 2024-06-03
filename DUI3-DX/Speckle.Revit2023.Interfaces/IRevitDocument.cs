#pragma warning disable CA1040

namespace Speckle.Revit2023.Interfaces;

public interface IRevitDocument
{
  string PathName { get; }
  IRevitUnits GetUnits();

  IRevitElement GetElement(IRevitElementId elementId);
}

public interface IRevitForgeTypeId { }

public interface IRevitElement
{
  IList<IRevitElementId> GetDependentElements(IRevitElementFilter filter);
  
  IRevitElementId Id { get; }
}

public interface IRevitHostObject : IRevitElement
{
  IList<IRevitElementId> FindInserts(bool addRectOpenings, bool includeShadows, bool includeEmbeddedWalls,
    bool includeSharedEmbeddedInserts);
}
public interface IRevitElementId
{
}
public interface IRevitCurtainGrid
{
  ICollection<IRevitElementId> GetMullionIds();
  ICollection<IRevitElementId> GetPanelIds();
}
public interface IRevitWall : IRevitHostObject
{
  IRevitCurtainGrid CurtainGrid { get; }
  bool IsStackedWall { get; }
  IList<IRevitElementId> GetStackedWallMemberIds();
}
