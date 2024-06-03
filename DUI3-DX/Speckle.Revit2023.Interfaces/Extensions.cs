namespace Speckle.Revit2023.Interfaces;

public static class Extensions
{
  public static IList<IRevitElementId> GetHostedElementIds(this IRevitElement host, IRevitFilterFactory revitFilterFactory)
  {
    IList<IRevitElementId> ids;
    if (host is IRevitHostObject hostObject)
    {
      ids = hostObject.FindInserts(true, false, false, false);
    }
    else
    {
      var typeFilter = revitFilterFactory.CreateElementIsElementTypeFilter(true);
      var categoryFilter = revitFilterFactory.CreateElementMulticategoryFilter(
        new List<RevitBuiltInCategory>()
        {
          RevitBuiltInCategory.OST_CLines,
          RevitBuiltInCategory.OST_SketchLines,
          RevitBuiltInCategory.OST_WeakDims
        },
        true
      );
      ids = host.GetDependentElements(revitFilterFactory.CreateLogicalAndFilter(typeFilter, categoryFilter));
    }

    // dont include host elementId
    ids.Remove(host.Id);

    return ids;
  }
}
