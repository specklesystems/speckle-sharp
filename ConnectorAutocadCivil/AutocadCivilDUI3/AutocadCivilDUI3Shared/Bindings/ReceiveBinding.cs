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
using System.Threading;
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
      CancellationTokenSource cts = CancellationManager.InitCancellationTokenSource(modelCardId);

      // 1 - Get receiver card
      ReceiverModelCard modelCard = _store.GetModelById(modelCardId) as ReceiverModelCard;

      // 2 - Get commit object from server
      Base commitObject = await Operations.GetCommitBase(Parent, modelCard, cts.Token).ConfigureAwait(true);

      if (cts.IsCancellationRequested)
      {
        throw new OperationCanceledException(cts.Token);
      }

      // 3 - Get converter
      ISpeckleConverter converter = Converters.GetConverter(Doc, Utils.VersionedAppName);

      // 4 - Traverse commit object
      List<Base> objectsToConvert = Traversal.GetObjectsToConvert(commitObject, converter);

      // 5 - Convert and Bake objects
      ConvertObjects(objectsToConvert, converter, modelCardId, cts);
      
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
