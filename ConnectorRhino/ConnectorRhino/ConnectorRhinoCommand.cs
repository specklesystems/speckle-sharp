using Rhino;
using Rhino.Commands;
using Rhino.PlugIns;
using Speckle.DesktopUI;
using Speckle.DesktopUI.Utils;
using System.Windows;

namespace SpeckleRhino
{
  public class SpeckleRhinoConnectorPlugin : PlugIn
  {
    public static SpeckleRhinoConnectorPlugin Instance { get; private set; }

    public SpeckleRhinoConnectorPlugin()
    {
      Instance = this;
      RhinoDoc.EndOpenDocument += RhinoDoc_EndOpenDocument;
      // RhinoApp.Idle += RhinoApp_Idle;
    }

    private void RhinoDoc_EndOpenDocument(object sender, DocumentOpenEventArgs e)
    {
      var bindings = new ConnectorBindingsRhino();
      if (bindings.GetStreamsInFile().Count > 0)
        SpeckleCommand.Instance.StartOrShowPanel();
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
        Bootstrapper.Application.MainWindow.Show();
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

      Bootstrapper.Setup(Application.Current);
      Bootstrapper.Start(new string[] { });

      Bootstrapper.Application.MainWindow.Initialized += (o, e) =>
      {
        ((ConnectorBindingsRhino)Bootstrapper.Bindings).GetFileContextAndNotifyUI();
      };

      Bootstrapper.Application.MainWindow.Closing += (object sender, System.ComponentModel.CancelEventArgs e) =>
      {
        Bootstrapper.Application.MainWindow.Hide();
        e.Cancel = true;
      };

      var helper = new System.Windows.Interop.WindowInteropHelper(Bootstrapper.Application.MainWindow);
      helper.Owner = RhinoApp.MainWindowHandle();
    }
  }

}
