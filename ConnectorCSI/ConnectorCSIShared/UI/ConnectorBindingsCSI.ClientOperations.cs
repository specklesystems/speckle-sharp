using System;
using System.Collections.Generic;
using ConnectorCSI.Storage;
using DesktopUI2;
using DesktopUI2.Models;
using Speckle.Core.Logging;

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
    try
    {
      return Model == null ? new List<StreamState>() : StreamStateManager.ReadState(Model);
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.Error(ex, "Error when retreiving streams in file");
      return new();
    }
  }

  #endregion
}
