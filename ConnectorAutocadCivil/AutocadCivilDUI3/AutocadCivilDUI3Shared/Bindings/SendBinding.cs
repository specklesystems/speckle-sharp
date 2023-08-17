using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using AutocadCivilDUI3Shared.Utils;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using DUI3;
using DUI3.Bindings;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace AutocadCivilDUI3Shared.Bindings
{
  public class SendBinding : IBinding
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

    public async void Send(string modelCardId)
    {
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
              var args = new SenderProgress()
              {
                Id = modelCardId,
                Status = "Converting",
                Progress = (double)count / objectsIds.Count
              };
              Parent.SendToBrowser(SendBindingEvents.SenderProgress, args);
            }
            catch (Exception)
            {
              continue;
            }
          }
        }
      }

      var commitObject = new Base();
      commitObject["@elements"] = convertedObjects;

      var projectId = model.ProjectId;
      Account account = AccountManager.GetAccounts().Where(acc => acc.id == model.AccountId).FirstOrDefault();
      var client = new Client(account);

      var transports = new List<ITransport> { new ServerTransport(client.Account, projectId) };

      var objectId = await Operations.Send(
        commitObject,
        transports,
        disposeTransports: true
      ).ConfigureAwait(true);

      Parent.SendToBrowser(SendBindingEvents.CreateVersion, new CreateVersion() { AccountId = account.id, ModelId = model.ModelId, ProjectId = model.ProjectId, ObjectId = objectId, Message = "Test", HostApp = "Autocad" });
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
