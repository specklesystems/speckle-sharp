using System.Collections.Generic;
using DesktopUI2;
using DesktopUI2.Models;
using ConnectorCSI.Storage;

namespace Speckle.ConnectorCSI.UI;

public partial class ConnectorBindingsCSI : ConnectorBindings
{
  #region Local stream I/O with local file
  public override List<MenuItem> GetCustomStreamMenuItems()
  {
    return new List<MenuItem>();
  }

  public override void WriteStreamsToFile(List<StreamState> streams)
  {
    StreamStateManager.ClearStreamStateList(Model);
    StreamStateManager.WriteStreamStateList(Model, streams);
  }

  public override List<StreamState> GetStreamsInFile()
  {
    return Model == null ? new List<StreamState>() : StreamStateManager.ReadState(Model);
  }

  #endregion
}
