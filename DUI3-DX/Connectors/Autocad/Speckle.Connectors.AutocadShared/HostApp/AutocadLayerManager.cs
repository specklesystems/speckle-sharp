using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.LayerManager;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Speckle.Connectors.Autocad.HostApp;

/// <summary>
/// Expects to be a scoped dependency for a given operation and helps with layer creation and cleanup.
/// </summary>
public class AutocadLayerManager
{
  private readonly AutocadContext _autocadContext;
  private readonly string _layerFilterName = "Speckle";

  // POC: Will be addressed to move it into AutocadContext!
  private Document Doc => Application.DocumentManager.MdiActiveDocument;
  private readonly HashSet<string> _uniqueLayerNames = new();

  public AutocadLayerManager(AutocadContext autocadContext)
  {
    _autocadContext = autocadContext;
  }

  /// <summary>
  /// Will create a layer with the provided name, or, if it finds an existing one, will "purge" all objects from it.
  /// This ensures we're creating the new objects we've just received rather than overlaying them.
  /// </summary>
  /// <param name="layerName">Name to search layer for purge and create.</param>
  public void CreateLayerForReceive(string layerName)
  {
    if (!_uniqueLayerNames.Add(layerName))
    {
      return;
    }

    Doc.LockDocument();
    using Transaction transaction = Doc.TransactionManager.StartTransaction();

    LayerTable? layerTable =
      transaction.TransactionManager.GetObject(Doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
    LayerTableRecord layerTableRecord = new() { Name = layerName };

    bool hasLayer = layerTable != null && layerTable.Has(layerName);
    if (hasLayer)
    {
      TypedValue[] tvs = { new((int)DxfCode.LayerName, layerName) };
      SelectionFilter selectionFilter = new(tvs);
      SelectionSet selectionResult = Doc.Editor.SelectAll(selectionFilter).Value;
      if (selectionResult == null)
      {
        return;
      }
      foreach (SelectedObject selectedObject in selectionResult)
      {
        transaction.GetObject(selectedObject.ObjectId, OpenMode.ForWrite).Erase();
      }

      return;
    }

    layerTable?.UpgradeOpen();
    layerTable?.Add(layerTableRecord);
    transaction.AddNewlyCreatedDBObject(layerTableRecord, true);
    transaction.Commit();
  }

  public void DeleteAllLayersByPrefix(string prefix)
  {
    Doc.LockDocument();
    using Transaction transaction = Doc.TransactionManager.StartTransaction();

    var layerTable = (LayerTable)transaction.TransactionManager.GetObject(Doc.Database.LayerTableId, OpenMode.ForRead);
    foreach (var layerId in layerTable)
    {
      var layer = (LayerTableRecord)transaction.GetObject(layerId, OpenMode.ForRead);
      var layerName = layer.Name;
      if (layer.Name.Contains(prefix))
      {
        // Delete objects from this layer
        TypedValue[] tvs = { new((int)DxfCode.LayerName, layerName) };
        SelectionFilter selectionFilter = new(tvs);
        SelectionSet selectionResult = Doc.Editor.SelectAll(selectionFilter).Value;
        if (selectionResult == null)
        {
          return;
        }
        foreach (SelectedObject selectedObject in selectionResult)
        {
          transaction.GetObject(selectedObject.ObjectId, OpenMode.ForWrite).Erase();
        }
        // Delete layer
        layer.UpgradeOpen();
        layer.Erase();
      }
    }
    transaction.Commit();
  }

  /// <summary>
  /// Creates a layer filter for the just received model, grouped under a top level filter "Speckle". Note: manual close and open of the layer properties panel required (it's an acad thing).
  /// This comes in handy to quickly access the layers created for this specific model.
  /// </summary>
  /// <param name="projectName"></param>
  /// <param name="modelName"></param>
  public void CreateLayerFilter(string projectName, string modelName)
  {
    using var docLock = Doc.LockDocument();
    string filterName = _autocadContext.RemoveInvalidChars($@"{projectName}-{modelName}");
    LayerFilterTree layerFilterTree = Doc.Database.LayerFilters;
    LayerFilterCollection? layerFilterCollection = layerFilterTree.Root.NestedFilters;
    LayerFilter? groupFilter = null;

    // Find existing layer filter if exists
    foreach (LayerFilter existingFilter in layerFilterCollection)
    {
      if (existingFilter.Name == _layerFilterName)
      {
        groupFilter = existingFilter;
        break;
      }
    }

    // Create new one unless exists
    if (groupFilter == null)
    {
      groupFilter = new LayerFilter() { Name = "Speckle", FilterExpression = $"NAME==\"SPK-*\"" };
      layerFilterCollection.Add(groupFilter);
    }

    string layerFilterExpression = $"NAME==\"SPK-{filterName}*\"";
    foreach (LayerFilter lf in groupFilter.NestedFilters)
    {
      if (lf.Name == filterName)
      {
        lf.FilterExpression = layerFilterExpression;
        return;
      }
    }
    var layerFilter = new LayerFilter() { Name = filterName, FilterExpression = layerFilterExpression };
    groupFilter.NestedFilters.Add(layerFilter);
    Doc.Database.LayerFilters = layerFilterTree;
  }

  /// <summary>
  /// Gets a valid layer name for a given context.
  /// </summary>
  /// <param name="context"></param>
  /// <param name="baseLayerPrefix"></param>
  /// <returns></returns>
  public string GetLayerPath(TraversalContext context, string baseLayerPrefix)
  {
    string[] collectionBasedPath = context.GetAscendantOfType<Collection>().Select(c => c.name).Reverse().ToArray();
    string[] path = collectionBasedPath.Length != 0 ? collectionBasedPath : context.GetPropertyPath().ToArray();

    var name = baseLayerPrefix + string.Join("-", path);
    return _autocadContext.RemoveInvalidChars(name);
  }
}
