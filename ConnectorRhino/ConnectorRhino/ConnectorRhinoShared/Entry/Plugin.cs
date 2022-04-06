using System;
using System.Collections.Generic;
using System.Linq;

using Rhino;
using Rhino.Commands;
using Rhino.PlugIns;

namespace SpeckleRhino
{
  public class SpeckleRhinoConnectorPlugin : PlugIn
  {
    public static SpeckleRhinoConnectorPlugin Instance { get; private set; }

    private List<string> ExistingStreams = new List<string>(); // property for tracking stream data during copy and import operations

    private static string SpeckleKey = "speckle2";

    public SpeckleRhinoConnectorPlugin()
    {
      Instance = this;
      RhinoDoc.BeginOpenDocument += RhinoDoc_BeginOpenDocument;
      RhinoDoc.EndOpenDocument += RhinoDoc_EndOpenDocument;
      SpeckleCommand.InitAvalonia();
    }

    private void RhinoDoc_EndOpenDocument(object sender, DocumentOpenEventArgs e)
    {
      if (e.Merge) // this is a paste or import event
      {
        // get incoming streams
        var incomingStreams = e.Document.Strings.GetEntryNames(SpeckleKey);

        // remove any that don't already exist in the current active doc
        foreach (var incomingStream in incomingStreams)
          if (!ExistingStreams.Contains(incomingStream))
            RhinoDoc.ActiveDoc.Strings.Delete(SpeckleKey, incomingStream);

        // skip binding
        return;
      }

      var bindings = new ConnectorBindingsRhino();
      if (bindings.GetStreamsInFile().Count > 0)
        SpeckleCommand.CreateOrFocusSpeckle();
    }

    private void RhinoDoc_BeginOpenDocument(object sender, DocumentOpenEventArgs e)
    {
      if (e.Merge) // this is a paste or import event
      {
        // get existing streams in doc before a paste or import operation to use for cleanup
        ExistingStreams = RhinoDoc.ActiveDoc.Strings.GetEntryNames(SpeckleKey).ToList();
      }
    }

    public override PlugInLoadTime LoadTime => PlugInLoadTime.AtStartup;
  }
}
