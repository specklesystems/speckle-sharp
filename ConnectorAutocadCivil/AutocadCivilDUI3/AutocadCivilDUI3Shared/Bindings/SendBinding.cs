using System;
using System.Collections.Generic;
using System.Linq;
using AutocadCivilDUI3Shared.Utils;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using DUI3;
using DUI3.Bindings;
using DUI3.Operations;
using DUI3.Utils;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Operations = Speckle.Core.Api.Operations;

namespace AutocadCivilDUI3Shared.Bindings
{
  public class SendBinding : ISendBinding, ICancelable
  {
    public string Name { get; set; } = "sendBinding";

    public IBridge Parent { get; set; }

    private AutocadDocumentModelStore _store;

    private HashSet<string> _changedObjectIds { get; set; } = new();

    public SendBinding(AutocadDocumentModelStore store)
    {
      _store = store;

      Database db = HostApplicationServices.WorkingDatabase;
      db.ObjectAppended += (sender, e) => OnChangeChangedObjectIds(e.DBObject);
      db.ObjectErased += (sender, e) => OnChangeChangedObjectIds(e.DBObject);
      db.ObjectModified += (sender, e) => OnChangeChangedObjectIds(e.DBObject);
    }

    private void OnChangeChangedObjectIds(DBObject dBObject)
    {
      if (!_store.IsDocumentInit) return;
      _changedObjectIds.Add(dBObject.Id.ToString());
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
        if (CancellationManager.IsExist(modelCardId))
        {
          CancellationManager.CancelOperation(modelCardId);
        }
        var cts = CancellationManager.InitCancellationTokenSource(modelCardId);

        SenderModelCard model = _store.GetModelById(modelCardId) as SenderModelCard;
        List<string> objectsIds = model.SendFilter.GetObjectIds();

        Document doc = Application.DocumentManager.MdiActiveDocument;
        var converter = KitManager.GetDefaultKit().LoadConverter(Utils.Utils.VersionedAppName);
        converter.SetContextDocument(doc);

        // TODO: Reject here deleted elements

        var convertedObjects = new List<Base>();

        using (DocumentLock acLckDoc = doc.LockDocument())
        {
          using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
          {
            var count = 0;
            foreach (var autocadObjectHandle in objectsIds)
            {
              if (cts.IsCancellationRequested)
              {
                tr.Commit();
                Progress.Cancel(Parent, modelCardId, (double)count / objectsIds.Count);
                return;
              }

              count++;
              // get the db object from id
              DBObject obj = null;
              string layer = null;
              string applicationId = null;
              if (Utils.Utils.GetHandle(autocadObjectHandle, out Handle hn))
              {
                obj = hn.GetObject(tr, out string type, out layer, out applicationId);
              }
              else
              {
                continue;
              }

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
                double progress = (double)count / objectsIds.Count;
                Progress.SenderProgressToBrowser(Parent, modelCardId, progress);
              }
              catch (Exception)
              {
                continue;
              }
            }

            tr.Commit();
          }
        }
        if (cts.IsCancellationRequested)
        {
          Progress.Cancel(Parent, modelCardId);
          return;
        }

        var commitObject = new Base();
        commitObject["@elements"] = convertedObjects;

        var projectId = model.ProjectId;
        Account account = AccountManager.GetAccounts().Where(acc => acc.id == model.AccountId).FirstOrDefault();
        var client = new Client(account);

        var transports = new List<ITransport> { new ServerTransport(client.Account, projectId) };

        // Pass null progress value to let UI swooshing progress bar
        Progress.SerializerProgressToBrowser(Parent, modelCardId, null);
        var objectId = await Operations.Send(
            commitObject,
            cts.Token,
            transports,
            disposeTransports: true
          )
          .ConfigureAwait(true);
        // Pass 1 progress value to let UI finish progress
        Progress.SerializerProgressToBrowser(Parent, modelCardId, 1);

        Parent.SendToBrowser(
          SendBindingEvents.CreateVersion,
          new CreateVersion()
          {
            AccountId = account.id,
            ModelId = model.ModelId,
            ModelCardId = modelCardId,
            ProjectId = model.ProjectId,
            ObjectId = objectId,
            Message = "Test",
            SourceApplication = "Autocad"
          });
      }
      catch (Exception e)
      {
        if (e is OperationCanceledException)
        {
          Progress.Cancel(Parent, modelCardId);
        }
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
      var objectIdsList = _changedObjectIds.ToArray();
      var expiredSenderIds = new List<string>();

      foreach (var sender in senders)
      {
        var isExpired = sender.SendFilter.CheckExpiry(objectIdsList);
        if (isExpired) expiredSenderIds.Add(sender.Id);
      }

      Parent.SendToBrowser(SendBindingEvents.SendersExpired, expiredSenderIds);
      _changedObjectIds = new HashSet<string>();
    }
  }
}
