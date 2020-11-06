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

    public SpeckleCommand()
    {
    }

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
      var bootstrapper = new Bootstrapper()
      {
        Bindings = new ConnectorBindingsRhino(doc)
      };

      if (Application.Current == null) new Application();

      var app = Application.Current;

      var rhApp = Rhino.RhinoApp.MainWindowHandle();
      bootstrapper.Setup(Application.Current);
      bootstrapper.Start(new string[] { });

      var helper = new System.Windows.Interop.WindowInteropHelper(bootstrapper.Application.MainWindow);
      helper.Owner = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;

      return Result.Success;
    }
  }

}
