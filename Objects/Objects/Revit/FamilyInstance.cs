using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Revit
{
  public class FamilyInstance : Element
  {
    public string family { get; set; }
    public bool flipped { get; set; }
    public Element host { get; set; }
    public Dictionary<string, object> parameters { get; set; }
  }
}
