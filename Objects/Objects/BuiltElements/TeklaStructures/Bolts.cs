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

    public double boltSize { get; set; }
    public string boltStandard { get; set; }
    public double cutLength { get; set; }
    public List<Point> coordinates { get; set; }
        public List<string> boltedPartsIds { get; set; } = new List<string>(); // First guid is PartToBeBolted, second guid is PartToBoltTo, any others are OtherPartsToBolt
    public Bolts() { }

  }
}
