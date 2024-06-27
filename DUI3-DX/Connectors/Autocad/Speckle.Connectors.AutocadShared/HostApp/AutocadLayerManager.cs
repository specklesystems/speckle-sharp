using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.LayerManager;

namespace Speckle.Connectors.Autocad.HostApp;

public class AutocadLayerManager
{
  private readonly AutocadContext _autocadContext;
  private readonly string _layerFilterName = "Speckle";

  // POC: Will be addressed to move it into AutocadContext!
  private Document Doc => Application.DocumentManager.MdiActiveDocument;

  public AutocadLayerManager(AutocadContext autocadContext)
  {
    _autocadContext = autocadContext;
  }

  /// <summary>
  /// Constructs layer name with prefix and valid characters.
  /// </summary>
  /// <param name="baseLayerPrefix"> Prefix to add layer name.</param>
  /// <param name="path"> list of entries to concat with hyphen.</param>
  /// <returns>Full layer name with provided prefix and path.</returns>
  public string LayerFullName(string baseLayerPrefix, string path)
  {
    var layerFullName = baseLayerPrefix + string.Join("-", path);
    return _autocadContext.RemoveInvalidChars(layerFullName);
  }

  /// <summary>
  /// Will create a layer with the provided name, or, if it finds an existing one, will "purge" all objects from it.
  /// This ensures we're creating the new objects we've just received rather than overlaying them.
  /// </summary>
  /// <param name="layerName">Name to search layer for purge and create.</param>
  public void CreateLayerOrPurge(string layerName)
  {
    // POC: Will be addressed to move it into AutocadContext!
    Document doc = Application.DocumentManager.MdiActiveDocument;
    doc.LockDocument();
    using Transaction transaction = doc.TransactionManager.StartTransaction();

    LayerTable? layerTable =
      transaction.TransactionManager.GetObject(doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
    LayerTableRecord layerTableRecord = new() { Name = layerName };

    bool hasLayer = layerTable != null && layerTable.Has(layerName);
    if (hasLayer)
    {
      TypedValue[] tvs = { new((int)DxfCode.LayerName, layerName) };
      SelectionFilter selectionFilter = new(tvs);
      SelectionSet selectionResult = doc.Editor.SelectAll(selectionFilter).Value;
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

  // POC: Consider to extract somehow in factory or service!
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
}
