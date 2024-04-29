using Autodesk.Revit.DB;

namespace Speckle.Converters.RevitShared.Extensions;

public static class ElementExtensions
{
  public static IList<ElementId> GetHostedElementIds(this Element host)
  {
    IList<ElementId> ids;
    if (host is HostObject hostObject)
    {
      ids = hostObject.FindInserts(true, false, false, false);
    }
    else
    {
      var typeFilter = new ElementIsElementTypeFilter(true);
      var categoryFilter = new ElementMulticategoryFilter(
        new List<BuiltInCategory>()
        {
          BuiltInCategory.OST_CLines,
          BuiltInCategory.OST_SketchLines,
          BuiltInCategory.OST_WeakDims
        },
        true
      );
      ids = host.GetDependentElements(new LogicalAndFilter(typeFilter, categoryFilter));
    }

    // dont include host elementId
    ids.Remove(host.Id);

    return ids;
  }
}
