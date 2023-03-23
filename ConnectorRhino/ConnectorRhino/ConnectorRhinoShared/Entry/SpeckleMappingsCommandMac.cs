using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using DesktopUI2.ViewModels.MappingTool;
using DesktopUI2.Views;
using Rhino;
using Rhino.Commands;
using Serilog;
using Speckle.Core.Logging;
using Speckle.Core.Models.Extensions;

namespace SpeckleRhino
{
#if MAC
  public class SpeckleMappingsCommandMac : Command
  {

    public static SpeckleMappingsCommandMac Instance { get; private set; }

    public override string EnglishName => "SpeckleMappings";
    public static Window MainWindow { get; private set; }

    public SpeckleMappingsCommandMac()
    {
      Instance = this;
    }

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {

      try
      {
        if (MainWindow == null)
        {
          var viewModel = new MappingsViewModel(SpeckleRhinoConnectorPlugin.Instance.MappingBindings);
          MainWindow = new MappingsWindow
          {
            DataContext = viewModel
          };
        }

        MainWindow.Show();
        MainWindow.Activate();
        return Result.Success;
      }
      catch (Exception e)
      {
        SpeckleLog.Logger.Fatal(e, "Failed to create or focus Speckle mappings window");
        RhinoApp.CommandLineOut.WriteLine($"Speckle Error - {e.ToFormattedString()}");
        return Result.Failure;
      }
    }



  }
#endif
}
