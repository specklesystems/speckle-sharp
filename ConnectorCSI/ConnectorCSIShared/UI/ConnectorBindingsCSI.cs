using System;
using System.Collections.Generic;
using DesktopUI2;
using Speckle.Core.Models;
using CSiAPIv1;
using Speckle.Core.Kits;

namespace Speckle.ConnectorCSI.UI;

public partial class ConnectorBindingsCSI : ConnectorBindings
{
  public static cSapModel Model { get; set; }
  public List<Exception> Exceptions { get; set; } = new List<Exception>();

  public ConnectorBindingsCSI(cSapModel model)
  {
    Model = model;
  }

  public override List<ReceiveMode> GetReceiveModes()
  {
    return new List<ReceiveMode> { ReceiveMode.Update, ReceiveMode.Create };
  }

  #region boilerplate
  public override string GetActiveViewName()
  {
    throw new NotImplementedException();
  }

  public override string GetDocumentId() => GetDocHash();

  private string GetDocHash() =>
    Utilities.HashString(Model.GetModelFilepath() + Model.GetModelFilename(), Utilities.HashingFunctions.MD5);

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

  public override void ResetDocument()
  {
    // TODO!
  }

  #endregion
}
