using System;
using System.Collections.Generic;
using DesktopUI2;
using DesktopUI2.Models;
using static DesktopUI2.ViewModels.MappingViewModel;
using Speckle.Core.Models;
using Speckle.ConnectorTeklaStructures.Util;
using System.Timers;
using Tekla.Structures.Model;
using Speckle.Core.Kits;

namespace Speckle.ConnectorTeklaStructures.UI
{
  public partial class ConnectorBindingsTeklaStructures : ConnectorBindings

  {
    public void OpenTeklaStructures()
    {
      var streams = GetStreamsInFile();
      if (UpdateSavedStreams != null)
        UpdateSavedStreams(streams);
    }
    public static Model Model { get; set; }
    public List<Exception> Exceptions { get; set; } = new List<Exception>(); //What is this? and can we get rid of it?
    public List<StreamState> DocumentStreams { get; set; } = new List<StreamState>();


    public ConnectorBindingsTeklaStructures(Model model)
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

    private string GetDocHash() => Utilities.hashString(Model.GetInfo().ModelPath + Model.GetInfo().ModelName, Utilities.HashingFuctions.MD5);

    public override string GetDocumentLocation() => Model.GetInfo().ModelPath;

    public override string GetFileName() => Model.GetInfo().ModelName;

    public override string GetHostAppNameVersion() => ConnectorTeklaStructuresUtils.TeklaStructuresAppName;
    public override string GetHostAppName() => HostApplications.TeklaStructures.Slug;

    public override List<string> GetObjectsInView()
    {
      throw new NotImplementedException();
    }

    public override async System.Threading.Tasks.Task<Dictionary<string, List<MappingValue>>> ImportFamilyCommand(Dictionary<string, List<MappingValue>> Mapping)
    {
      await System.Threading.Tasks.Task.Delay(TimeSpan.FromMilliseconds(500));
      return new Dictionary<string, List<MappingValue>>();
    }
    #endregion
  }
}
