using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;
using Objects.Geometry;

namespace Objects.BuiltElements.TeklaStructures
{
  public class Bolts : Base
  {
    public Mesh displayMesh { get; set; }

    public double BoltSize { get; set; }
    public string BoltStandard { get; set; }
    public double CutLength { get; set; }
    public List<Point> Coordinates { get; set; }
    public Bolts() { }

  }
}
