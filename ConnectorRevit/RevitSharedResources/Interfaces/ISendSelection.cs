using System.Collections.Generic;
using DB = Autodesk.Revit.DB;

namespace RevitSharedResources.Interfaces
{
  /// <summary>
  /// This object is responsible for creating and exposing a list of <see cref="DB.Element"/>s that corrosponds
  /// to the user's selection of objects to send.
  /// </summary>
  public interface ISendSelection
  {
    IReadOnlyCollection<DB.Element> Elements { get; }
    bool ContainsElementWithId(string uniqueId);
  }
}
