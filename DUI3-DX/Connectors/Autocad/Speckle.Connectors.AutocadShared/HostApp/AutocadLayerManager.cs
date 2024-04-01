using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.LayerManager;

namespace Speckle.Connectors.Autocad.HostApp;

public class AutocadLayerManager
{
  private readonly AutocadContext _autocadContext;
  private Document Doc => Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;

  public AutocadLayerManager(AutocadContext autocadContext)
  {
    _autocadContext = autocadContext;
  }

  public string LayerFullName(string baseLayerPrefix, string path)
  {
    var layerFullName = baseLayerPrefix + string.Join("-", path);
    return _autocadContext.RemoveInvalidChars(layerFullName);
  }

  /// <summary>
  /// Will create a layer with the provided name, or, if it finds an existing one, will "purge" all objects from it.
  /// This ensures we're creating the new objects we've just received rather than overlaying them.
  /// </summary>
  /// <param name="layerName"></param>
  public void CreateLayerOrPurge(string layerName)
  {
    using var transaction = Doc.TransactionManager.TopTransaction;

    var layerTable =
      transaction.TransactionManager.GetObject(Doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
    var layerTableRecord = new LayerTableRecord() { Name = layerName };

    var hasLayer = layerTable.Has(layerName);
    if (hasLayer)
    {
      var tvs = new[] { new TypedValue((int)DxfCode.LayerName, layerName) };
      var selectionFilter = new SelectionFilter(tvs);
      var selectionResult = Doc.Editor.SelectAll(selectionFilter).Value;
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

    layerTable.UpgradeOpen();
    layerTable.Add(layerTableRecord);
    transaction.AddNewlyCreatedDBObject(layerTableRecord, true);
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
    var filterName = _autocadContext.RemoveInvalidChars($@"{projectName}-{modelName}");
    var layerFilterTree = Doc.Database.LayerFilters;
    var layerFilterCollection = layerFilterTree.Root.NestedFilters;
    var groupFilterName = "Speckle";
    LayerFilter groupFilter = null;

    foreach (LayerFilter existingFilter in layerFilterCollection)
    {
      if (existingFilter.Name == groupFilterName)
      {
        groupFilter = existingFilter;
        break;
      }
    }

    if (groupFilter == null)
    {
      groupFilter = new LayerFilter() { Name = "Speckle", FilterExpression = $"NAME==\"SPK-*\"" };
      layerFilterCollection.Add(groupFilter);
    }

    var layerFilterExpression = $"NAME==\"SPK-{filterName}*\"";
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
}
