using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using DUI3.Operations;
using DUI3.Settings;
using DUI3.Utils;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using ICancelable = DUI3.Operations.ICancelable;

namespace ConnectorRhinoWebUI.Bindings
{
  public class ReceiveBinding : IReceiveBinding, ICancelable
  {
    public string Name { get; set; } = "receiveBinding";
    public IBridge Parent { get; set; }

    private readonly DocumentModelStore _store;

    private RhinoDoc Doc => RhinoDoc.ActiveDoc;

    public CancellationManager CancellationManager { get; } = new();

    public ReceiveBinding(DocumentModelStore store)
    {
      _store = store;
    }

    public void CancelReceive(string modelCardId)
    {
      CancellationManager.CancelOperation(modelCardId);
    }

    public async void Receive(string modelCardId, string versionId)
    {
      try
      {
        // 0 - Init cancellation token source -> Manager also cancel it if exist before
        var cts = CancellationManager.InitCancellationTokenSource(modelCardId);

        // 1 - Get receiver card
        ReceiverModelCard model = _store.GetModelById(modelCardId) as ReceiverModelCard;

        // 2 - Get commit object from server
        Base commitObject = await Operations.GetCommitBase(Parent, model, versionId, cts.Token).ConfigureAwait(true);

        if (cts.IsCancellationRequested)
          return;

        // 3 - Get converter
        ISpeckleConverter converter = Converters.GetConverter(Doc, "Rhino7");

        // 4 - Traverse commit object
        List<Base> objectsToConvert = Traversal.GetObjectsToConvert(commitObject, converter);

        // 5 - Convert bases to Rhino preview objects
        List<object> objectsToBake = ConvertPreviewObjects(objectsToConvert, converter, modelCardId, cts);

        // 6 - Bake preview objects to RhinoObject
        BakeObjects(objectsToBake, Doc, modelCardId, cts);

        // 7 - Redraw the view to render baked objects
        Doc.Views.Redraw();
      }
      catch (Exception e)
      {
        if (e is OperationCanceledException)
        {
          Progress.CancelReceive(Parent, modelCardId);
          return;
        }
        // TODO: Init here class to handle send errors to report UI, Seq etc..
        throw;
      }
    }

    public List<CardSetting> GetReceiveSettings()
    {
      return new List<CardSetting>()
      {
        new()
        {
          Id = "mergeCoplanarFaces",
          Title = "Merge Coplanar Faces",
          Value = true,
          Type = "boolean"
        },
        new()
        {
          Id = "receiveMode",
          Title = "Receive Mode",
          Value = "Update",
          Type = "string",
          Enum = new List<string>() { "Update", "Create", "Ignore" }
        }
      };
    }

    // conversion and bake
    private List<object> ConvertObject(Base obj, ISpeckleConverter converter)
    {
      var convertedList = new List<object>();

      var converted = converter.ConvertToNative(obj);
      if (converted == null)
        return convertedList;

      //Iteratively flatten any lists
      void FlattenConvertedObject(object item)
      {
        if (item is IList list)
          foreach (object child in list)
            FlattenConvertedObject(child);
        else
          convertedList.Add(item);
      }

      FlattenConvertedObject(converted);

      return convertedList;
    }

    private void BakeObjects(List<object> previewObjects, RhinoDoc doc, string modelCardId, CancellationTokenSource cts)
    {
      int bakedCount = 0;
      foreach (var convertedItem in previewObjects)
      {
        if (cts.IsCancellationRequested)
        {
          Progress.CancelReceive(Parent, modelCardId, (double)bakedCount / previewObjects.Count);
          return;
        }
        switch (convertedItem)
        {
          case GeometryBase o:
            var attributes = new ObjectAttributes();

            Guid id = doc.Objects.Add(o, attributes);

            bakedCount++;

            break;
          case RhinoObject o: // this was probably a block instance, baked during conversion ???

            bakedCount++;
            break;
          case ViewInfo o: // this is a view, baked during conversion ???

            bakedCount++;
            break;
        }
        Progress.ReceiverProgressToBrowser(Parent, modelCardId, (double)bakedCount / previewObjects.Count);
      }
      Progress.ReceiverProgressToBrowser(Parent, modelCardId, 1);
    }

    private List<object> ConvertPreviewObjects(
      List<Base> objectsToConvert,
      ISpeckleConverter converter,
      string modelCardId,
      CancellationTokenSource cts
    )
    {
      var objectsToBake = new List<object>();
      var errors = new List<string>();
      int count = 0;
      foreach (var objToConvert in objectsToConvert)
      {
        if (cts.IsCancellationRequested)
        {
          Progress.CancelReceive(Parent, modelCardId, (double)count / objectsToConvert.Count);
          break;
        }

        count++;
        double progress = (double)count / objectsToConvert.Count;
        Progress.ReceiverProgressToBrowser(Parent, modelCardId, progress);

        var objectsToAddBakeList = ConvertObject(objToConvert, converter);
        if (objectsToAddBakeList == null)
        {
          errors.Add($"Object couldn't converted with id: {objToConvert.id}, type: {objToConvert.speckle_type}\n");
        }
        else
        {
          objectsToBake.AddRange(objectsToAddBakeList);
        }
      }

      Notification.ReportReceive(Parent, errors, modelCardId, objectsToConvert.Count);
      return objectsToBake;
    }
  }
}
