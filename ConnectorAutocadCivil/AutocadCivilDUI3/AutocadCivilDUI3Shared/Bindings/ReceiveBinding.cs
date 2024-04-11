using System;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using AutocadCivilDUI3Shared.Utils;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.LayerManager;
using DUI3.Models.Card;
using DUI3.Operations;
using DUI3.Utils;

namespace ConnectorAutocadDUI3.Bindings;

public class ReceiveBinding : IReceiveBinding, ICancelable
{
  public string Name { get; set; } = "receiveBinding";
  public IBridge Parent { get; set; }

  private readonly DocumentModelStore _store;

  public CancellationManager CancellationManager { get; } = new();

  private Document Doc => Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;

  public ReceiveBinding(DocumentModelStore store)
  {
    _store = store;
  }

  public void CancelReceive(string modelCardId) => CancellationManager.CancelOperation(modelCardId);

  public async void Receive(string modelCardId)
  {
    try
    {
      // 0 - Init cancellation token source -> Manager also cancel it if exist before
      var cts = CancellationManager.InitCancellationTokenSource(modelCardId);

      // 1 - Get receiver card
      var modelCard = _store.GetModelById(modelCardId) as ReceiverModelCard;

      // 2 - Get commit object from server
      var commitObject = await Operations.GetCommitBase(Parent, modelCard, cts.Token).ConfigureAwait(true);

      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException(cts.Token);
      }

      // 3 - Get converter
      ISpeckleConverter converter = Converters.GetConverter(Doc, Utils.VersionedAppName);

      // 4 - Traverse commit object
      BasicConnectorBindingCommands.SetModelProgress(
        Parent,
        modelCardId,
        new ModelCardProgress() { Status = "Parsing structure" }
      );
      var objectsToConvert = new List<(List<string>, Base)>();
      foreach (var (objPath, obj) in commitObject.TraverseWithPath((obj) => obj is not Collection))
      {
        if (cts.IsCancellationRequested)
        {
          throw new OperationCanceledException(cts.Token);
        }
        if (obj is not Collection && converter.CanConvertToNative(obj))
        {
          objectsToConvert.Add((objPath, obj));
        }
      }

      if (objectsToConvert.Count == 0)
      {
        throw new Exception("No convertible objects found.");
      }

      var baseLayerPrefix = $"SPK-{modelCard.ProjectName}-{modelCard.ModelName}-";
      BasicConnectorBindingCommands.SetModelProgress(
        Parent,
        modelCardId,
        new ModelCardProgress() { Status = "Starting conversion" }
      );

      using var docLock = Doc.LockDocument();
      using var transaction = Doc.Database.TransactionManager.StartTransaction();

      CreateLayerFilter(modelCard.ProjectName, modelCard.ModelName);
      var convertedObjectIds = BakeObjects(objectsToConvert, baseLayerPrefix, modelCardId, cts, converter);

      Autodesk.AutoCAD.Internal.Utils.FlushGraphics();
      ReceiveBindingUiCommands.SetModelConversionResult(
        Parent,
        modelCardId,
        new ReceiveResult() { BakedObjectIds = convertedObjectIds, Display = true }
      );
      transaction.Commit();
    }
    catch (Exception e)
    {
      if (e is OperationCanceledException)
      {
        return;
      }
      BasicConnectorBindingCommands.SetModelError(Parent, modelCardId, e);
    }
  }

  private List<string> BakeObjects(
    List<(List<string>, Base)> objects,
    string baseLayerPrefix,
    string modelCardId,
    CancellationTokenSource cts,
    ISpeckleConverter converter
  )
  {
    BasicConnectorBindingCommands.SetModelProgress(
      Parent,
      modelCardId,
      new ModelCardProgress() { Status = "Converting" }
    );
    var uniqueLayerNames = new HashSet<string>();
    var handleValues = new List<string>();
    var count = 0;
    foreach (var (path, obj) in objects)
    {
      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException(cts.Token);
      }

      try
      {
        var layerFullName = baseLayerPrefix + string.Join("-", path);
        layerFullName = Utils.RemoveInvalidChars(layerFullName);

        if (uniqueLayerNames.Add(layerFullName))
        {
          CreateLayerOrPurge(layerFullName);
        }

        var converted = converter.ConvertToNative(obj);
        var flattened = Traversal.FlattenToNativeConversionResult(converted);
        foreach (Entity conversionResult in flattened.Cast<Entity>())
        {
          if (conversionResult == null)
          {
            continue;
          }

          conversionResult.Append(layerFullName);
          handleValues.Add(conversionResult.Handle.Value.ToString());
          BasicConnectorBindingCommands.SetModelProgress(
            Parent,
            modelCardId,
            new ModelCardProgress() { Status = "Converting", Progress = (double)++count / objects.Count }
          );
        }
      }
      catch (Exception e) // DO NOT CATCH SPECIFIC STUFF, conversion errors should be recoverable
      {
        // TODO: you know, report, etc.
        Debug.WriteLine("conversion error happened.");
      }
    }
    return handleValues;
  }

  /// <summary>
  /// Will create a layer with the provided name, or, if it finds an existing one, will "purge" all objects from it.
  /// This ensures we're creating the new objects we've just received rather than overlaying them.
  /// </summary>
  /// <param name="layerName"></param>
  private void CreateLayerOrPurge(string layerName)
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
  private void CreateLayerFilter(string projectName, string modelName)
  {
    using var docLock = Doc.LockDocument();
    var filterName = Utils.RemoveInvalidChars($@"{projectName}-{modelName}");
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
