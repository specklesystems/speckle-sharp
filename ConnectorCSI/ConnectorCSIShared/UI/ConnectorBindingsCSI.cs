using System;
using System.Collections.Generic;
using DesktopUI2;
using DesktopUI2.Models;
using Speckle.Core.Models;
using Speckle.ConnectorCSI.Util;
using System.Timers;
using CSiAPIv1;
using Speckle.Core.Kits;

namespace Speckle.ConnectorCSI.UI
{
  public partial class ConnectorBindingsCSI : ConnectorBindings
  {
    public static cSapModel Model { get; set; }
    public List<Exception> Exceptions { get; set; } = new List<Exception>();

    public Timer SelectionTimer;
    public List<StreamState> DocumentStreams { get; set; } = new List<StreamState>();


    public ConnectorBindingsCSI(cSapModel model)
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

    public override List<ReceiveMode> GetReceiveModes()
    {
      return new List<ReceiveMode> { ReceiveMode.Create };
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

    public override string GetHostAppNameVersion() => GetHostAppVersion(Model);
    public override string GetHostAppName() => GetHostAppName(Model);
    public string GetHostAppVersion(cSapModel model)
    {
      var name = "";
      var ver = "";
      var type = "";
      model.GetProgramInfo(ref name, ref ver, ref type);
      return name;
    }
    public string GetHostAppName(cSapModel model)
    {
      var name = "";
      var ver = "";
      var type = "";
      model.GetProgramInfo(ref name, ref ver, ref type);
      return name.ToLower();
    }
    public override List<string> GetObjectsInView()
    {
      throw new NotImplementedException();
    }


    #endregion





  }
}