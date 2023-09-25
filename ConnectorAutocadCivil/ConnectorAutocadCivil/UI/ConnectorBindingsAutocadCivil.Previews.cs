using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.Core.Kits;
using Speckle.Core.Models;

#if ADVANCESTEEL
using ASFilerObject = Autodesk.AdvanceSteel.CADAccess.FilerObject;
#endif

namespace Speckle.ConnectorAutocadCivil.UI
{
  public partial class ConnectorBindingsAutocad : ConnectorBindings
  {
    public override bool CanPreviewSend => true;

    public override async void PreviewSend(StreamState state, ProgressViewModel progress)
    {
      // report and converter
      progress.Report = new ProgressReport();
      var converter = KitManager.GetDefaultKit().LoadConverter(Utils.VersionedAppName);
      if (converter == null)
      {
        progress.Report.LogOperationError(new Exception("Could not load converter"));
        return;
      }
      converter.SetContextDocument(Doc);

      var filterObjs = GetObjectsFromFilter(state.Filter, converter);
      var existingIds = new List<string>();
      using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      {
        foreach (var id in filterObjs)
        {
          DBObject obj = null;
          string type = "";
          if (Utils.GetHandle(id, out Handle hn))
          {
            obj = hn.GetObject(tr, out type, out string layer, out string applicationId);
          }
          if (obj == null)
          {
            progress.Report.Log(
              new ApplicationObject(id, "unknown")
              {
                Status = ApplicationObject.State.Failed,
                Log = new List<string>() { "Could not find object in document" }
              }
            );
            continue;
          }

          var appObj = new ApplicationObject(id, type) { Status = ApplicationObject.State.Unknown };

          if (converter.CanConvertToSpeckle(obj))
          {
            appObj.Update(status: ApplicationObject.State.Created);
          }
          else
          {
#if ADVANCESTEEL
            UpdateASObject(appObj, obj);
#endif
            appObj.Update(
              status: ApplicationObject.State.Failed,
              logItem: "Object type conversion to Speckle not supported"
            );
          }

          progress.Report.Log(appObj);
          existingIds.Add(id);
        }
        tr.Commit();
      }

      if (existingIds.Count == 0)
      {
        progress.Report.LogOperationError(new Exception("No valid objects selected, nothing will be sent!"));
        return;
      }

      Doc.Editor.SetImpliedSelection(new ObjectId[0]);
      SelectClientObjects(existingIds);
    }

    public override bool CanPreviewReceive => false;

    public override async Task<StreamState> PreviewReceive(StreamState state, ProgressViewModel progress)
    {
      return null;
    }
  }
}
