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
    //placement of family instances
    public string placementType { get; set; }
    public bool hasFamilySymbol { get; set; }
    //shape of MEP elements
    public string shape { get; set; }

  }

}
