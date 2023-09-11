using System.Linq;
using System.Windows.Threading;
using DUI3;
using DUI3.Bindings;
using DUI3.Models;
using AutocadCivilDUI3Shared.Utils;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
using System.Collections.Generic;
using System.Collections;
using DUI3.Utils;

namespace ConnectorAutocadDUI3.Bindings
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
    
    public async void Receive(string modelCardId, string versionId)
    {
      Base commitObject = await DUI3.Utils.Receive.GetCommitBase(Parent, _store, modelCardId, versionId);

      Document doc = Application.DocumentManager.MdiActiveDocument;

      using (DocumentLock l = doc.LockDocument())
      {
        using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
        {
          var converter = KitManager.GetDefaultKit().LoadConverter(Utils.VersionedAppName);
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
          BakeObject(objectsToBake, converter, tr, null, null);

          Dispatcher.CurrentDispatcher.Invoke(() =>
          {
            Progress.ReceiverProgressToBrowser(Parent, modelCardId, 1);
          }, DispatcherPriority.Background);

          tr.Commit();
        }
      }

      Dispatcher.CurrentDispatcher.Invoke(() =>
      {
        
      }, DispatcherPriority.Background);
      
      
    }

    private void BakeObject(
      List<object> convertedItems,
      ISpeckleConverter converter,
      Transaction tr,
      string layer,
      List<ObjectId> toRemove,
      ApplicationObject parent = null
    )
    {
      int bakedCount = 0;
      foreach (var convertedItem in convertedItems)
      {
        switch (convertedItem)
        {
          case Entity o:
            var res = o.Append();
            if (res.IsValid)
            {
              tr.TransactionManager.QueueForGraphicsFlush();
              bakedCount++;
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

    public void CancelReceive(string modelCardId)
    {
      throw new System.NotImplementedException();
    }
  }
}
