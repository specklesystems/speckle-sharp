using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.BuiltElements.Revit
{
  public class RevitElementType : Base
  {
    public string family { get; set; }
    public string type { get; set; }
    public string category { get; set; }
    public string placementType { get; set; }
    public bool hasFamilySymbol { get; set; }

  }

}
