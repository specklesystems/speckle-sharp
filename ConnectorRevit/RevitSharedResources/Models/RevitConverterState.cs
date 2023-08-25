#nullable enable
using Autodesk.Revit.DB;
using Speckle.Core.Helpers;

namespace RevitSharedResources.Models;

public class RevitConverterState : State<RevitConverterState>
{
  public Element? CurrentHostElement { get; set; }
}
