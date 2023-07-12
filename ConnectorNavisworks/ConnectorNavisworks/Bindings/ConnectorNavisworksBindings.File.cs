using System.Collections.Generic;
using System.IO;
using DesktopUI2.Models;
using Sentry.Protocol;
using Speckle.ConnectorNavisworks.Storage;
using Application = Autodesk.Navisworks.Api.Application;

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

  public override string GetFileName()
  {
    var activeDoc = Application.ActiveDocument;

    if (activeDoc == null)
      throw (new FileNotFoundException("No active document found."));

    if (activeDoc.Models.Count == 0)
      throw (new FileNotFoundException("No models found in active document."));

    return Application.ActiveDocument.CurrentFileName ?? string.Empty;
  }
}
