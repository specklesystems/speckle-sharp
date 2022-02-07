using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

using Rhino;
using Rhino.Commands;
using Rhino.PlugIns;

using Speckle.DesktopUI;
using Speckle.DesktopUI.Utils;

namespace SpeckleRhino
{
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
