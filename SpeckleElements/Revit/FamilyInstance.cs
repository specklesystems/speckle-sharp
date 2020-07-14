using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Elements.Revit
{
  public class FamilyInstance : Element
  {
    public string family { get; set; }
    public bool flipped { get; set; }
    public Dictionary<string, object> parameters { get; set; }
  }
}
