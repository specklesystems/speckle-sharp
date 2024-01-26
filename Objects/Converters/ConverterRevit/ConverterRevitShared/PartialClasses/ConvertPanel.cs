using System.Collections.Generic;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  public ApplicationObject PanelToNative(RevitCurtainWallPanel specklePanel)
  {
    return new ApplicationObject(specklePanel.id, specklePanel.speckle_type)
    {
      Status = ApplicationObject.State.Skipped,
      Log = new List<string>() { "Revit does not support receive standalone curtain panels " }
    };
  }

  public RevitCurtainWallPanel PanelToSpeckle(DB.Panel revitPanel)
  {
    RevitCurtainWallPanel panel = new();
    return (RevitCurtainWallPanel)RevitElementToSpeckle(revitPanel, out _, panel);
  }
}
