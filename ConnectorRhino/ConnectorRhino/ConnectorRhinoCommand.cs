using System;
using Rhino;
using Rhino.Commands;
using Rhino.PlugIns;
using Speckle.DesktopUI;
using Speckle.DesktopUI.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SpeckleRhino
{
  public class SpeckleRhinoConnectorPlugin : PlugIn
  {
    public static SpeckleRhinoConnectorPlugin Instance { get; private set; }

    private List<string> ExistingStreams = new List<string>(); // property for tracking stream data during copy and import operations

    private static string SpeckleKey = "speckle";

    public SpeckleRhinoConnectorPlugin()
    {
      Instance = this;
      RhinoDoc.BeginOpenDocument += RhinoDoc_BeginOpenDocument;
      RhinoDoc.EndOpenDocument += RhinoDoc_EndOpenDocument;
      // RhinoApp.Idle += RhinoApp_Idle;
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
        SpeckleCommand.Instance.StartOrShowPanel();
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

  public class SpeckleCommand : Command
  {
    public static SpeckleCommand Instance { get; private set; }

    public override string EnglishName => "Speckle";

    public static Bootstrapper Bootstrapper { get; set; }

    public SpeckleCommand()
    {
      Instance = this;
    }

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
      StartOrShowPanel();
      return Result.Success;
    }

    internal void StartOrShowPanel()
    {
      if (Bootstrapper != null)
      {
        Bootstrapper.ShowRootView();
        return;
      }

      Bootstrapper = new Bootstrapper()
      {
        Bindings = new ConnectorBindingsRhino()
      };

      if (Application.Current == null)
      {
        new Application();
      }

      if ( Application.Current != null )
        new StyletAppLoader() {Bootstrapper = Bootstrapper};
      else
        new App(Bootstrapper);

      Bootstrapper.Start(Application.Current);
      Bootstrapper.SetParent(RhinoApp.MainWindowHandle());
    }
  }
}
