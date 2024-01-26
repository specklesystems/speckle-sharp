using System;
using System.Collections.Generic;
using DesktopUI2;
using DesktopUI2.Models;
using Speckle.Core.Models;
using Speckle.ConnectorTeklaStructures.Util;
using Tekla.Structures.Model;
using Speckle.Core.Kits;

namespace Speckle.ConnectorTeklaStructures.UI;

public partial class ConnectorBindingsTeklaStructures : ConnectorBindings
{
  public void OpenTeklaStructures()
  {
    var streams = GetStreamsInFile();
    if (UpdateSavedStreams != null)
    {
      UpdateSavedStreams(streams);
    }
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

  private string GetDocHash() =>
    Utilities.HashString(Model.GetInfo().ModelPath + Model.GetInfo().ModelName, Utilities.HashingFunctions.MD5);

  public override string GetDocumentLocation() => Model.GetInfo().ModelPath;

  public override string GetFileName() => Model.GetInfo().ModelName;

  public override string GetHostAppNameVersion() => ConnectorTeklaStructuresUtils.TeklaStructuresAppName;

  public override string GetHostAppName() => HostApplications.TeklaStructures.Slug;

  public override List<string> GetObjectsInView()
  {
    throw new NotImplementedException();
  }
  #endregion
}
