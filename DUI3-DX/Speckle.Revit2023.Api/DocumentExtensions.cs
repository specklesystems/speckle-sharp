using Speckle.Revit2023.Interfaces;
using DB = Autodesk.Revit.DB;

namespace Speckle.Revit2023.Api;

public static class DocumentExtensions
{
  public static DB.Element GetElement(this IRevitDocument document, DB.ElementId id)
  {
    return ((IRevitDocumentProxy)document)._Instance.GetElement(id);
  }
}
