using System;
using System.Collections.Generic;
using DesktopUI2;
using DesktopUI2.Models;
using static DesktopUI2.ViewModels.MappingViewModel;
using Speckle.Core.Models;
using Speckle.ConnectorCSI.Util;
using System.Timers;
using CSiAPIv1;
using Speckle.Core.Kits;
using System.Threading.Tasks;

namespace Speckle.ConnectorCSI.UI
{
  public partial class ConnectorBindingsCSI : ConnectorBindings
  {
    public static cSapModel Model { get; set; }
    public List<Exception> Exceptions { get; set; } = new List<Exception>();
    public List<StreamState> DocumentStreams { get; set; } = new List<StreamState>();
    public ConnectorBindingsCSI(cSapModel model)
    {
      Model = model;
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

    public override async Task<Dictionary<string, List<MappingValue>>> ImportFamilyCommand(Dictionary<string, List<MappingValue>> Mapping)
    {
      await Task.Delay(TimeSpan.FromMilliseconds(500));
      return new Dictionary<string, List<MappingValue>>();
    }

    public override void ResetDocument()
    {
      // TODO!
    }

    #endregion

  }
}