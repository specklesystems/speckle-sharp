using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitSharedResources.Interfaces
{
  public interface ISendSelection
  {
    IReadOnlyCollection<Element> Elements { get; }
    bool ContainsElementWithId(string uniqueId);
  }
}
