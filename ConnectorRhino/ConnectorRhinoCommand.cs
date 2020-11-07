using System;
using System.Runtime.InteropServices;
using Rhino;
using Rhino.Commands;
using RhinoWindows;
using Rhino.Input.Custom;
using Rhino.PlugIns;
using Rhino.UI;
using Speckle.DesktopUI;
using System.Drawing;
using System.Windows;

namespace SpeckleRhino
{
  public class RhinoConnector : PlugIn
  {
  }

  public class SpeckleCommand : Command
  {
    public static SpeckleCommand Instance { get; set; }

    public override string EnglishName => "Speckle";

    public static Bootstrapper Bootstrapper { get; set; }

    public SpeckleCommand()
    {
    }

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
      StartOrShowPanel(doc);
      return Result.Success;
    }

    private void StartOrShowPanel(RhinoDoc doc)
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

      if (Application.Current == null) new Application();

      Bootstrapper.Setup(Application.Current);
      Bootstrapper.Start(new string[] { });

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
