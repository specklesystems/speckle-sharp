using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Revit
{
  public class AdaptiveComponent : Element
  {
    public string family { get; set; }
    public bool flipped { get; set; }
    public Dictionary<string, object> parameters { get; set; }
  }
}
