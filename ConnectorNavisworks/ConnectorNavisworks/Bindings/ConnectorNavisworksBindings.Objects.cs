using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Navisworks.Api;
using DesktopUI2.Models.Filters;
using DesktopUI2.Models.Settings;
using static System.Tuple;
using static Speckle.ConnectorNavisworks.Utilities;
using Cursor = System.Windows.Forms.Cursor;

namespace Speckle.ConnectorNavisworks.Bindings;

public partial class ConnectorBindingsNavisworks
{
  public override List<string> GetObjectsInView() // this returns all visible doc objects.
  {
    var objects = new List<string>();
    return objects;
  }

  private static IEnumerable<Tuple<string, int>> GetObjectsFromFilter(ISelectionFilter filter)
  {
    var filteredObjects = new List<Tuple<string, int>>();

    switch (filter.Slug)
    {
      case "manual":
        filteredObjects.AddRange(GetObjectsFromSelection(filter));
        return filteredObjects;

      case "sets":
        filteredObjects.AddRange(GetObjectsFromSavedSets(filter));
        return filteredObjects;

      case "clashes":
        filteredObjects.AddRange(GetObjectsFromClashResults(filter));
        return filteredObjects;

      case "views":
        filteredObjects.AddRange(GetObjectsFromSavedViewpoint(filter));
        return filteredObjects;

      default:
        return filteredObjects;
    }
  }

  private static IEnumerable<Tuple<string, int>> GetObjectsFromSavedViewpoint(ISelectionFilter filter)
  {
    var selection = filter.Selection.FirstOrDefault();
    if (string.IsNullOrEmpty(selection))
      return Enumerable.Empty<Tuple<string, int>>();

    var savedViewpoint = ResolveSavedViewpoint(selection);
    if (savedViewpoint == null || !savedViewpoint.ContainsVisibilityOverrides)
      return Enumerable.Empty<Tuple<string, int>>();

    var items = savedViewpoint.GetVisibilityOverrides().Hidden;
    items.Invert(_doc);

    var uniqueIds = new Dictionary<string, int>();

    for (var i = 0; i < items.Count; i += 1)
    {
      if (!IsElementVisible(items[i]))
        continue;
      uniqueIds.Add(GetPseudoId(items[i]), VisibleDescendantsCount(items[i]));
    }

    return uniqueIds.Select(kv => Create(kv.Key, kv.Value)).ToList();
  }

  private static SavedViewpoint ResolveSavedViewpoint(string savedViewReference)
  {
    var flattenedViewpointList = _doc.SavedViewpoints.RootItem.Children
      .Select(GetViews)
      .Where(x => x != null)
      .SelectMany(node => node.Flatten())
      .Select(node => new { Reference = node?.Reference?.Split(':'), node?.Guid })
      .ToList();

    var viewpointMatch = flattenedViewpointList.FirstOrDefault(
      node =>
        node.Guid.ToString() == savedViewReference
        || node.Reference?.Length == 2 && node.Reference[1] == savedViewReference
    );

    return viewpointMatch == null
      ? null
      // Resolve the SavedViewpoint
      : ResolveSavedViewpoint(viewpointMatch, savedViewReference);
  }

  private static SavedViewpoint ResolveSavedViewpoint(dynamic viewpointMatch, string savedViewReference)
  {
    if (Guid.TryParse(savedViewReference, out var guid))
      // Even though we may have already got a match, that could be to a generic Guid from earlier versions of Navisworks
      if (savedViewReference != new Guid().ToString())
        return (SavedViewpoint)_doc.SavedViewpoints.ResolveGuid(guid);

    if (!(viewpointMatch?.Reference is string[] reference) || reference.Length != 2)
      return null;

    using var savedRef = new SavedItemReference(reference[0], reference[1]);
    using var resolvedReference = _doc.ResolveReference(savedRef);
    return (SavedViewpoint)resolvedReference;
  }

  private static IEnumerable<Tuple<string, int>> GetObjectsFromSelection(ISelectionFilter filter)
  {
    bool fullTreeSetting;
    ProgressBar.Update(0);
    try
    {
      fullTreeSetting = CurrentSettings.Find(x => x.Slug == "full-tree") is CheckBoxSetting { IsChecked: true };
    }
    catch (ArgumentNullException)
    {
      fullTreeSetting = false;
    }

    var selection = filter.Selection;
    var uniquePseudoIds = new HashSet<string>();
    var items = selection.Select(PointerToModelItem).Where(IsElementVisible).ToList();

    var ancestorCount = 0.0;

    if (fullTreeSetting)
    {
      var ancestors = items.SelectMany(item => item.Ancestors).Distinct();
      foreach (var ancestor in ancestors)
        if (IsElementVisible(ancestor) && uniquePseudoIds.Add(GetPseudoId(ancestor)))
        {
          ancestorCount += 1;
          yield return Create(GetPseudoId(ancestor), 1);
        }
    }

    var descendantsAndSelves = items.SelectMany(item => item.DescendantsAndSelf.Where(IsElementVisible)).ToList();

    foreach (var descendant in descendantsAndSelves.Where(descendant => uniquePseudoIds.Add(GetPseudoId(descendant))))
    {
      yield return Create(GetPseudoId(descendant), 1);
      var done = uniquePseudoIds.Count / (descendantsAndSelves.Count + ancestorCount);
      ProgressBar.Update(done);
    }

    // ProgressBar.Update(1);
  }

  private static IEnumerable<Tuple<string, int>> GetObjectsFromSavedSets(ISelectionFilter filter)
  {
    Cursor.Current = Cursors.WaitCursor;
    // Saved Sets filter stores Guids of the selection sets. This can be converted to ModelItem pseudoIds
    var selections = filter.Selection.Select(guid => new Guid(guid)).ToList();
    var savedItems = selections.Select(_doc.SelectionSets.ResolveGuid).Cast<SelectionSet>().ToList();

    var objectPseudoIds = new Dictionary<string, int>();

    savedItems.ForEach(item =>
    {
      if (item.HasExplicitModelItems)
      {
        var nodes = item.ExplicitModelItems;
        if (nodes != null)
          objectPseudoIds = MergeDictionaries(objectPseudoIds, nodes);
      }

      if (item.HasSearch)
      {
        var nodes = item.Search.FindAll(_doc, false);
        if (nodes != null)
          objectPseudoIds = MergeDictionaries(objectPseudoIds, nodes);
      }
    });

    Cursor.Current = Cursors.Default;
    return objectPseudoIds.Select(kv => Create(kv.Key, kv.Value)).ToList();
  }

  private static int VisibleDescendantsCount(ModelItem modelItem)
  {
    return modelItem.Descendants.Count(IsElementVisible);
  }

  private static Func<ModelItem, Tuple<string, int>> IdAndCountFunc()
  {
    return x => new Tuple<string, int>(GetPseudoId(x), VisibleDescendantsCount(x));
  }

  private static Dictionary<string, int> MergeDictionaries(
    Dictionary<string, int> dict1,
    ModelItemCollection modelItemsCollection
  )
  {
    var dict2 = modelItemsCollection
      .Where(IsElementVisible)
      .Select(IdAndCountFunc())
      .ToDictionary(t => t.Item1, t => t.Item2);

    return dict1.Concat(dict2).GroupBy(kv => kv.Key).ToDictionary(g => g.Key, g => g.First().Value);
  }

  // ReSharper disable once UnusedParameter.Local
  private static IEnumerable<Tuple<string, int>> GetObjectsFromClashResults(ISelectionFilter unused)
  {
    // Clash Results filter stores Guid of the Clash Result groups per Test. This can be converted to ModelItem pseudoIds
    throw new NotImplementedException();
  }
}
