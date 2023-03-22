using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using DesktopUI2.Views;
using Rhino;
using Rhino.Commands;
using Serilog;
using Speckle.Core.Logging;
using Speckle.Core.Models.Extensions;

namespace SpeckleRhino
{
  #if MAC
  public class SpeckleCommandMac : Command
  {

    public static SpeckleCommandMac Instance { get; private set; }

    public override string EnglishName => "Speckle";

    public static Window MainWindow { get; private set; }

    public SpeckleCommandMac()
    {
      Instance = this;
    }

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {

      try
      {
        CreateOrFocusSpeckle();
        return Result.Success;
      }
      catch (Exception e)
      {
        SpeckleLog.Logger.Fatal(e, "Failed to create or focus Speckle window");
        RhinoApp.CommandLineOut.WriteLine($"Speckle Error - { e.ToFormattedString() }");
        return Result.Failure;
      }
    }

    public static void CreateOrFocusSpeckle()
    {
      //SpeckleRhinoConnectorPlugin.Instance.Init();
      if (MainWindow == null)
      {
        var viewModel = new MainViewModel(SpeckleRhinoConnectorPlugin.Instance.Bindings);
        MainWindow = new MainWindow
        {
          DataContext = viewModel
        };
      }

      MainWindow.Show();
      MainWindow.Activate();
    }
  }
#endif
}
