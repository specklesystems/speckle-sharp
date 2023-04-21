using System;
using Rhino;
using Rhino.Commands;
using Rhino.UI;
using Speckle.Core.Models.Extensions;

namespace SpeckleRhino;

#if !MAC
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
    catch (Exception e)
    {
      RhinoApp.CommandLineOut.WriteLine($"Speckle Error - {e.ToFormattedString()}");
      return Result.Failure;
    }
  }
}
#endif
