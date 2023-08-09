using System;
using System.Collections.Generic;
using System.Linq;
using AutocadCivilDUI3Shared.Utils;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using DUI3;
using DUI3.Bindings;

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
