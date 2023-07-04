using DesktopUI2;
using Rhino;
using Rhino.DocObjects.Tables;

namespace SpeckleRhino;

public partial class ConnectorBindingsRhino : ConnectorBindings
{
  private void RhinoDoc_EndOpenDocument(object sender, DocumentOpenEventArgs e)
  {
    if (e.Merge)
      return; // prevents triggering this on copy pastes, imports, etc.

    if (e.Document == null)
      return;

    var streams = GetStreamsInFile();
    if (UpdateSavedStreams != null)
      UpdateSavedStreams(streams);

    ClearStorage();
    //if (streams.Count > 0)
    //  SpeckleCommand.CreateOrFocusSpeckle();
  }

  private void RhinoDoc_LayerChange(object sender, LayerTableEventArgs e)
  {
    if (UpdateSelectedStream != null)
      UpdateSelectedStream();
  }
}
