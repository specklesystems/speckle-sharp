using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using DUI3.Utils;
using Objects.Converter.RhinoGh;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace ConnectorRhinoWebUI.Bindings
{
  public class ReceiveBinding : IReceiveBinding
  {
    public string Name { get; set; } = "receiveBinding";
    public IBridge Parent { get; set; }

    private DocumentModelStore _store;

    public ReceiveBinding(DocumentModelStore store)
    {
      _store = store;
    }

    public void CancelReceive(string modelCardId)
    {
      throw new NotImplementedException();
    }

    public async void Receive(string modelCardId, string versionId)
    {
      RhinoDoc doc = RhinoDoc.ActiveDoc;
      Base commitObject = await DUI3.Utils.Receive.GetCommitBase(Parent, _store, modelCardId, versionId);

      RhinoApp.InvokeOnUiThread(
        (Action)(
          () =>
          {
            ConverterRhinoGh converter = new ConverterRhinoGh();
            converter.SetContextDocument(doc);

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
              count++;
              double progress = (double)count / objectsToConvert.Count;
              Dispatcher.CurrentDispatcher.Invoke(() =>
              {
                Progress.ReceiverProgressToBrowser(Parent, modelCardId, progress);          
              }, DispatcherPriority.Background);
              
              var objectsToAddBakeList = ConvertObject(objectToConvert, converter);
              objectsToBake.AddRange(objectsToAddBakeList);
            }
            
            BakeObject(objectsToBake, converter, null, doc);

            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
              Progress.ReceiverProgressToBrowser(Parent, modelCardId, 1);          
            }, DispatcherPriority.Background);

            doc.Views.Redraw();
          }));
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
      return @object.speckle_type.Contains("Instance")
          || @object.speckle_type.Contains("View")
          || @object.speckle_type.Contains("Collection");
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
