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
using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using Autodesk.AutoCAD.EditorInput;
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
      BasicConnectorBindingCommands.SetModelProgress(Parent, modelCardId, new ModelCardProgress() { Status = "Parsing structure" });
      var objectsToConvert2 = new List<(List<string>,Base)>();
      foreach (var (objPath, obj) in commitObject.TraverseWithPath((obj) => obj is not Collection))
      {
        if (cts.IsCancellationRequested)
        {
          throw new OperationCanceledException(cts.Token);
        }
        if (obj is not Collection && converter.CanConvertToNative(obj))
        {
          objectsToConvert2.Add((objPath, obj));
        }
      }

      var baseLayerPrefix = $"Speckle - Project {modelCard.ProjectName} - Model {modelCard.ModelName} - "; // TODO VALIDATE NAME
      var convertedObjectIds = BakeObjects2(objectsToConvert2, baseLayerPrefix, modelCardId, cts, converter);
      
      
      // Previous steps
      // List<Base> objectsToConvert = Traversal.GetObjectsToConvert(commitObject, converter);
      //
      // // 5 - Convert and Bake objects
      // ConvertObjects(objectsToConvert, converter, modelCardId, cts);
      
      // TODO: FUCK AROUND AND FIND OUT
      // Namely, get a receive result in the local state and to the UI
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

  private List<string> BakeObjects2(List<(List<string>,Base)> objects, string baseLayerPrefix, string modelCardId, CancellationTokenSource cts, ISpeckleConverter converter)
  {
    // Prep layers
    BasicConnectorBindingCommands.SetModelProgress(Parent, modelCardId, new ModelCardProgress() { Status = "Creating layers" });
    var uniqueLayerNames = new HashSet<string>();
    
    using var docLock = Doc.LockDocument();
    using var transaction = Doc.Database.TransactionManager.StartTransaction();
    
    var handleValues = new List<string>();
    foreach (var (path, obj) in objects)
    {
      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException(cts.Token);
      }
      try
      {
        var layerFullName = baseLayerPrefix + string.Join("-", path);
        layerFullName = Utils.RemvoeInvalidChars(layerFullName);
        
        if (uniqueLayerNames.Add(layerFullName))
        {
          CreateLayerOrPurge(layerFullName);
        }
        
        var converted = converter.ConvertToNative(obj);
        var flattened = Traversal.FlattenToNativeConversionResult(converted);
        foreach (Entity conversionResult in flattened)
        {
          conversionResult.Append(layerFullName);
          handleValues.Add(conversionResult.Handle.Value.ToString());
        }
      }
      catch (Exception e) // DO NOT CATCH SPECIFIC STUFF, conversion errors should be recoverable
      {
        Debug.WriteLine("conversion error happened.");
      }
    }
    transaction.Commit();
    
    return handleValues;
    // TODO: remember we have to return HANDLES
  }

  private void CreateLayerOrPurge(string layerName)
  {
    using var transaction = Doc.TransactionManager.TopTransaction;
    var layerTable = transaction.TransactionManager.GetObject(Doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
    var layerTableRecord = new LayerTableRecord() { Name = layerName };

    var hasLayer = layerTable.Has(layerName);
    if (hasLayer)
    {
      var tvs = new[] { new TypedValue((int)DxfCode.LayerName, layerName) };
      var selectionFilter = new SelectionFilter(tvs);
      var selectionResult = Doc.Editor.SelectAll(selectionFilter).Value;
      foreach (SelectedObject selectedObject in selectionResult)
      {
        transaction.GetObject(selectedObject.ObjectId, OpenMode.ForWrite).Erase();
      }
      
      return;
    }
    
    layerTable.UpgradeOpen();
    var id = layerTable.Add(layerTableRecord);
    transaction.AddNewlyCreatedDBObject(layerTableRecord, true);
  }
  
  private void ConvertObjects(
    List<Base> objectsToConvert,
    ISpeckleConverter converter,
    string modelCardId,
    CancellationTokenSource cts
  )
  {
    using DocumentLock l = Doc.LockDocument();
    using Transaction tr = Doc.Database.TransactionManager.StartTransaction();
    List<object> objectsToBake = new();

    int count = 0;
    foreach (Base objectToConvert in objectsToConvert)
    {
      if (cts.IsCancellationRequested)
      {
        tr.Commit();
        throw new OperationCanceledException(cts.Token);
      }
      count++;
      double progress = (double)count / objectsToConvert.Count;
      BasicConnectorBindingCommands.SetModelProgress(Parent, modelCardId, new ModelCardProgress() {Status="Converting", Progress = progress});

      List<object> objectsToAddBakeList = ConvertObject(objectToConvert, converter);
      objectsToBake.AddRange(objectsToAddBakeList);
    }
    BakeObjects(objectsToBake, tr);

    BasicConnectorBindingCommands.SetModelProgress(Parent, modelCardId, new ModelCardProgress() {Status="Conversion done", Progress = 1});
    tr.Commit();
  }

  private void BakeObjects(List<object> convertedItems, Transaction tr)
  {
    // int bakedCount = 0;
    foreach (object convertedItem in convertedItems)
    {
      switch (convertedItem)
      {
        case Entity o:
          ObjectId res = o.Append();
          if (res.IsValid)
          {
            tr.TransactionManager.QueueForGraphicsFlush();
            // bakedCount++;
          }
          else
          {
            continue;
          }
          break;
        default:
          break;
      }
    }
  }

  // conversion and bake
  private List<object> ConvertObject(Base obj, ISpeckleConverter converter)
  {
    List<object> convertedList = new();

    object converted = converter.ConvertToNative(obj);
    if (converted == null)
    {
      return convertedList;
    }

    // Iteratively flatten any lists
    void FlattenConvertedObject(object item)
    {
      if (item is IList list)
      {
        foreach (object child in list)
        {
          FlattenConvertedObject(child);
        }
      }
      else
      {
        convertedList.Add(item);
      }
    }
    FlattenConvertedObject(converted);

    return convertedList;
  }
}
