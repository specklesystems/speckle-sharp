using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AutocadCivilDUI3Shared.Utils;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using DUI3;
using DUI3.Bindings;
using DUI3.Operations;
using DUI3.Utils;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace AutocadCivilDUI3Shared.Bindings
{
  public class SendBinding : ISendBinding, ICancelable
  {
    public string Name { get; set; } = "sendBinding";

    public IBridge Parent { get; set; }

    private readonly AutocadDocumentModelStore _store;

    private Document Doc => Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;

    private HashSet<string> ChangedObjectIds { get; set; } = new();

    public SendBinding(AutocadDocumentModelStore store)
    {
      _store = store;

      Database db = HostApplicationServices.WorkingDatabase;
      db.ObjectAppended += (_, e) => OnChangeChangedObjectIds(e.DBObject);
      db.ObjectErased += (_, e) => OnChangeChangedObjectIds(e.DBObject);
      db.ObjectModified += (_, e) => OnChangeChangedObjectIds(e.DBObject);
    }

    private void OnChangeChangedObjectIds(DBObject dBObject)
    {
      if (!_store.IsDocumentInit) return;
      ChangedObjectIds.Add(dBObject.Id.ToString());
      RunExpirationChecks();
    }

    public List<ISendFilter> GetSendFilters()
    {
      return new List<ISendFilter>()
      {
        new AutocadEverythingFilter(),
        new AutocadSelectionFilter()
      };
    }

    public CancellationManager CancellationManager { get; } = new();

    public async void Send(string modelCardId)
    {
      try
      {
        // 0 - Init cancellation token source -> Manager also cancel it if exist before
        var cts = CancellationManager.InitCancellationTokenSource(modelCardId);

        // 1 - Get model
        SenderModelCard model = _store.GetModelById(modelCardId) as SenderModelCard;
      
        // 2 - Check account exist
        Account account = Accounts.GetAccount(model.AccountId);
        
        // 3 - Get elements to convert
        List<DBObject> dbObjects = GetObjectsFromDocument(model);
        
        // 4 - Get converter
        ISpeckleConverter converter = Converters.GetConverter(Doc, Utils.Utils.VersionedAppName);

        // 5 - Convert objects
        Base commitObject = ConvertObjects(dbObjects, converter, modelCardId, cts);

        if (cts.IsCancellationRequested) return;

        // 6 - Get transports
        var transports = new List<ITransport> { new ServerTransport(account, model.ProjectId) };

        // 7 - Serialize and Send objects
        string objectId = await Operations.Send(Parent, modelCardId, commitObject, cts.Token, transports).ConfigureAwait(true);
      
        if (cts.IsCancellationRequested) return;

        // 8 - Create Version
        Operations.CreateVersion(Parent, model, objectId, "Autocad");
      }
      catch (Exception e)
      {
        if (e is OperationCanceledException)
        {
          Progress.CancelSend(Parent, modelCardId);
          return;
        }
        // TODO: Init here class to handle send errors to report UI, Seq etc..
        throw;
      }
    }

    public void CancelSend(string modelCardId)
    {
      CancellationManager.CancelOperation(modelCardId);
    }

    public void Highlight(string modelCardId)
    {
      throw new NotImplementedException();
    }

    private void RunExpirationChecks()
    {
      var senders = _store.GetSenders();
      var objectIdsList = ChangedObjectIds.ToArray();
      var expiredSenderIds = new List<string>();

      foreach (var sender in senders)
      {
        var isExpired = sender.SendFilter.CheckExpiry(objectIdsList);
        if (isExpired) expiredSenderIds.Add(sender.Id);
      }

      Parent.SendToBrowser(SendBindingEvents.SendersExpired, expiredSenderIds);
      ChangedObjectIds = new HashSet<string>();
    }
    
    private Base ConvertObjects(List<DBObject> dbObjects, ISpeckleConverter converter, string modelCardId, CancellationTokenSource cts)
    {
      var commitObject = new Base();
    
      var convertedObjects = new List<Base>();
      int count = 0;
      foreach (DBObject obj in dbObjects)
      {
        if (cts.IsCancellationRequested)
        {
          Progress.CancelSend(Parent, modelCardId, (double)count / dbObjects.Count);
          break;
        }
        count++;

        try
        {
          // convert obj
          Base converted = converter.ConvertToSpeckle(obj);
          if (converted == null)
          {
            // TODO: report!
            continue;
          }

          convertedObjects.Add(converted);
          double progress = (double)count / dbObjects.Count;
          Progress.SenderProgressToBrowser(Parent, modelCardId, progress);
        }
        catch
        {
          // FIXME: Figure it out why it's happening!
          continue;
        }
      }
    
      commitObject["@elements"] = convertedObjects;

      return commitObject;
    }

    private List<DBObject> GetObjectsFromDocument(SenderModelCard model)
    {
      List<string> objectsIds = model.SendFilter.GetObjectIds();
      var dbObjects = new List<DBObject>();
      using DocumentLock acLckDoc = Doc.LockDocument();
      using Transaction tr = Doc.Database.TransactionManager.StartTransaction();
      foreach (var autocadObjectHandle in objectsIds)
      {
        // TODO: also provide here cancel operation
        // get the db object from id
        if (!Utils.Utils.GetHandle(autocadObjectHandle, out Handle hn)) continue;
        DBObject obj = hn.GetObject(tr, out string _, out string _, out string _);
        dbObjects.Add(obj);
      }
      tr.Commit();

      return dbObjects;
    }
  }
}
