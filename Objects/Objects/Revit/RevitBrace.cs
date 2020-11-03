using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Revit
{
  public class RevitBrace : Beam
  {
    public string family { get; set; }
    public Dictionary<string, object> parameters { get; set; }
  }
}
