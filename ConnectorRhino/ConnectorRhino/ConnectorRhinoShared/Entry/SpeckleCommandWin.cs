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
using Speckle.Core.Models.Extensions;

namespace SpeckleRhino
{
#if !MAC
  public class SpeckleCommandWin : Command
  {

    public static SpeckleCommandWin Instance { get; private set; }

    public override string EnglishName => "Speckle";

    public SpeckleCommandWin()
    {
      Instance = this;
    }

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {

      try
      {

        Rhino.UI.Panels.OpenPanel(typeof(DuiPanel).GUID);
        return Result.Success;
      }
      catch (Exception e)
      {
        RhinoApp.CommandLineOut.WriteLine($"Speckle Error - { e.ToFormattedString() }");
        return Result.Failure;
      }
    }

  }
#endif
}
