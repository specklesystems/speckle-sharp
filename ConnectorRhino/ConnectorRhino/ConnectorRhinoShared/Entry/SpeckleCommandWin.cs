#if !MAC
using System;
using Rhino;
using Rhino.Commands;
using Rhino.UI;
using Speckle.Core.Logging;
using Speckle.Core.Models.Extensions;

namespace SpeckleRhino;

public class SpeckleCommandWin : Command
{
  public SpeckleCommandWin()
  {
    Instance = this;
  }

  public static SpeckleCommandWin Instance { get; private set; }

  public override string EnglishName => "Speckle";

  protected override Result RunCommand(RhinoDoc doc, RunMode mode)
  {
    try
    {
      Panels.OpenPanel(typeof(DuiPanel).GUID);
      return Result.Success;
    }
    catch (Exception e) when (!e.IsFatal())
    {
      // needs more investigation. logging to seq for now.
      SpeckleLog.Logger.Error(e, "Failed to open Speckle Rhino Connector DuiPanel with {exceptionMessage}", e.Message);
      RhinoApp.CommandLineOut.WriteLine($"Speckle Error - {e.ToFormattedString()}");
      return Result.Failure;
    }
  }
}

#endif
