using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.Extensions;

public static class ElementExtensions
{
  // POC: should this be an injected service?
  public static IList<IRevitElementId> GetHostedElementIds(this IRevitElement host, IRevitFilterFactory filterFactory)
  {
    IList<IRevitElementId> ids;
    var hostObject = host.ToHostObject();
    if (hostObject is not null)
    {
      ids = hostObject.FindInserts(true, false, false, false);
    }
    else
    {
      var typeFilter = filterFactory.CreateElementIsElementTypeFilter(true);
      var categoryFilter = filterFactory.CreateElementMulticategoryFilter(
        new List<RevitBuiltInCategory>()
        {
          RevitBuiltInCategory.OST_CLines,
          RevitBuiltInCategory.OST_SketchLines,
          RevitBuiltInCategory.OST_WeakDims
        },
        true
      );
      ids = host.GetDependentElements( filterFactory.CreateLogicalAndFilter(typeFilter, categoryFilter));
    }

    // dont include host elementId
    ids.Remove(host.Id);

    return ids;
  }
}