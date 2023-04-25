using System.Collections.Generic;
using DesktopUI2.Models;
using Speckle.ConnectorNavisworks.Storage;

namespace Speckle.ConnectorNavisworks.Bindings;

public partial class ConnectorBindingsNavisworks
{
  public override void WriteStreamsToFile(List<StreamState> streams)
  {
    SpeckleStreamManager.WriteStreamStateList(_doc, streams);
  }

  public override List<StreamState> GetStreamsInFile()
  {
    var streams = new List<StreamState>();
    if (_doc != null)
      streams = SpeckleStreamManager.ReadState(_doc);
    return streams;
  }
}
