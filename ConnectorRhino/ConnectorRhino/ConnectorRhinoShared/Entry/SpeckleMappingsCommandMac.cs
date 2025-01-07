#if MAC
using System;
using Avalonia.Controls;
using DesktopUI2.ViewModels.MappingTool;
using DesktopUI2.Views;
using Rhino;
using Rhino.Commands;
using Speckle.Core.Logging;
using Speckle.Core.Models.Extensions;

namespace SpeckleRhino;

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
        MainWindow = new MappingsWindow { DataContext = viewModel };
      }

      MainWindow.Show();
      MainWindow.Activate();
      return Result.Success;
    }
    catch (Exception e) when (!e.IsFatal())
    {
      SpeckleLog.Logger.Fatal(e, "Failed to create or focus Speckle mappings window");
      RhinoApp.CommandLineOut.WriteLine($"Speckle Error - {e.ToFormattedString()}");
      return Result.Failure;
    }
  }
}
#endif
