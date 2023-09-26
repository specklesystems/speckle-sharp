using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using DUI3.Operations;
using DUI3.Utils;
using Objects.Converter.RhinoGh;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
using ICancelable = DUI3.Operations.ICancelable;

namespace ConnectorRhinoWebUI.Bindings
{
  public class ReceiveBinding : IReceiveBinding, ICancelable
  {
    public string Name { get; set; } = "receiveBinding";
    public IBridge Parent { get; set; }

    private DocumentModelStore _store;

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
      if (CancellationManager.IsExist(modelCardId))
      {
        CancellationManager.CancelOperation(modelCardId);
      }

      var cts = CancellationManager.InitCancellationTokenSource(modelCardId);

      RhinoDoc doc = RhinoDoc.ActiveDoc;
      Base commitObject = await DUI3.Utils.Receive.GetCommitBase(Parent, _store, cts.Token, modelCardId, versionId);

      ConverterRhinoGh converter = new ConverterRhinoGh();
      converter.SetContextDocument(doc);

      var errors = new List<string>();

      var traverseFunction = DefaultTraversal.CreateTraverseFunc(converter);

      var objectsToConvert = traverseFunction
        .Traverse(commitObject)
        .Select(
          tc => tc.current) // Previously we were creating ApplicationObject, now just returning Base object.
        .Reverse()
        .ToList();

      var objectsToBake = new List<object>();

      int count = 0;
      foreach (var objectToConvert in objectsToConvert)
      {
        if (cts.IsCancellationRequested)
        {
          Progress.SenderProgressToBrowser(Parent, modelCardId, 1);
          return;
        }
        count++;
        double progress = (double)count / objectsToConvert.Count;
        Progress.ReceiverProgressToBrowser(Parent, modelCardId, progress);

        var objectsToAddBakeList = ConvertObject(objectToConvert, converter);
        if (objectsToAddBakeList == null)
        {
          errors.Add(
            string.Format(
              "Object couldn't converted with id: {0}, type: {1}\n",
              objectToConvert.id,
              objectToConvert.speckle_type));
        }

        objectsToBake.AddRange(objectsToAddBakeList);
      }

      BakeObject(objectsToBake, converter, null, doc);

      Progress.ReceiverProgressToBrowser(Parent, modelCardId, 1);

      doc.Views.Redraw();

      ReportToUI(errors, modelCardId, objectsToConvert.Count);
    }

    private void ReportToUI(List<string> errors, string modelCardId, int numberOfObject)
    {
      if (errors.Any())
      {
        Parent.SendToBrowser(
          ReceiveBindingEvents.Notify,
          new ToastInfo()
          {
            ModelCardId = modelCardId,
            Text = string.Join("\n", errors),
            Level = "warning",
            Timeout = 5000
          });
      }
      else
      {
        Parent.SendToBrowser(
          ReceiveBindingEvents.Notify,
          new ToastInfo()
          {
            ModelCardId = modelCardId,
            Text = string.Format("Speckle objects ({0}) are received successfully.", numberOfObject),
            Level = "success",
            Timeout = 5000
          });
      }
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

    private static bool IsPreviewIgnore(Base @object)
    {
      return @object.speckle_type.Contains("Instance") ||
             @object.speckle_type.Contains("View") ||
             @object.speckle_type.Contains("Collection");
    }

    private void BakeObject(
      List<object> convertedItems,
      ISpeckleConverter converter,
      Layer layer,
      RhinoDoc doc,
      ApplicationObject parent = null
    )
    {
      int bakedCount = 0;
      foreach (var convertedItem in convertedItems)
      {
        switch (convertedItem)
        {
          case GeometryBase o:
            var attributes = new ObjectAttributes();

            Guid id = doc.Objects.Add(o, attributes);

            bakedCount++;

            break;
          case RhinoObject o: // this was prbly a block instance, baked during conversion ???

            bakedCount++;
            break;
          case ViewInfo o: // this is a view, baked during conversion ???

            bakedCount++;
            break;
        }
      }
    }
  }
}
