using System;
using System.Collections.Generic;
using DesktopUI2;
using DesktopUI2.Models;
using Speckle.Core.Models;
using Speckle.ConnectorETABS.Util;
using System.Timers;
using ETABSv1;
using Speckle.Core.Kits;

namespace Speckle.ConnectorETABS.UI
{
  public partial class ConnectorBindingsETABS : ConnectorBindings
  {
    public static cSapModel Model { get; set; }
    public List<Exception> Exceptions { get; set; } = new List<Exception>();

    public Timer SelectionTimer;
    public List<StreamState> DocumentStreams { get; set; } = new List<StreamState>();


    public ConnectorBindingsETABS(cSapModel model)
    {
      Model = model;
      SelectionTimer = new Timer(2000) { AutoReset = true, Enabled = true };
      SelectionTimer.Elapsed += SelectionTimer_Elapsed;
      SelectionTimer.Start();
    }

    private void SelectionTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
      if (Model == null)
      {
        return;
      }

      var selection = GetSelectedObjects();
      //TO DO


      //NotifyUi(new UpdateSelectionCountEvent() { SelectionCount = selection.Count });
      //NotifyUi(new UpdateSelectionEvent() { ObjectIds = selection });
    }




    #region boilerplate
    public override string GetActiveViewName()
    {
      throw new NotImplementedException();
    }

    public override string GetDocumentId() => GetDocHash();

    private string GetDocHash() => Utilities.hashString(Model.GetModelFilepath() + Model.GetModelFilename(), Utilities.HashingFuctions.MD5);

    public override string GetDocumentLocation() => Model.GetModelFilepath();

    public override string GetFileName() => Model.GetModelFilename();

    public override string GetHostAppNameVersion() => ConnectorETABSUtils.ETABSAppName;
    public override string GetHostAppName() => HostApplications.ETABS.Slug;

    public override List<string> GetObjectsInView()
    {
      throw new NotImplementedException();
    }


    #endregion





  }
}
