#if !MAC

using System;
using Rhino;
using Rhino.Commands;
using Rhino.UI;
using Speckle.Core.Logging;
using Speckle.Core.Models.Extensions;

namespace SpeckleRhino;

public class SpeckleMappingsCommandWin : Command
{
  public SpeckleMappingsCommandWin()
  {
    Instance = this;
  }

  public static SpeckleMappingsCommandWin Instance { get; private set; }

  public override string EnglishName => "SpeckleMappings";

  protected override Result RunCommand(RhinoDoc doc, RunMode mode)
  {
    try
    {
      Panels.OpenPanel(typeof(MappingsPanel).GUID);
      return Result.Success;
    }
    catch (Exception e) when (!e.IsFatal())
    {
      // needs more investigation. logging to seq for now.
      SpeckleLog.Logger.Error(e, "Failed to open Speckle Rhino Mapper DuiPanel with {exceptionMessage}", e.Message);
      RhinoApp.CommandLineOut.WriteLine($"Speckle Error - {e.ToFormattedString()}");
      return Result.Failure;
    }
  }
}
#endif
