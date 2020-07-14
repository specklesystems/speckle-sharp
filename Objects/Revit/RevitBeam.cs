using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Objects.Revit
{
  public class RevitBeam : Beam
  {
    public string family { get; set; }
    public Dictionary<string, object> parameters { get; set; }
  }
}
