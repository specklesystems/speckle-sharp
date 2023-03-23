using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;
using Speckle.Core.Models;

namespace Objects.Structural.CSI.Geometry
{
  public class CSIGridLines : Base
  {
    public CSIGridLines()
    {
    }

    public double Xo { get; set; }
    public double Yo { get; set; }
    public double Rz { get; set; }
    public string GridSystemType { get; set; }
    [DetachProperty]
    public List<GridLine> gridLines { get; set; }

  }
}
