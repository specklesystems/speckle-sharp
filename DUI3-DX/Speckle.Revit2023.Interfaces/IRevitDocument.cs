#pragma warning disable CA1040

namespace Speckle.Revit2023.Interfaces;

public interface IRevitDocument
{
  string PathName { get; }
  IRevitUnits GetUnits();
}

public interface IRevitForgeTypeId { }
