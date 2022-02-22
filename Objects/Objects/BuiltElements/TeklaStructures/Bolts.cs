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
        public Point firstPosition { get; set; }
        public Point secondPosition { get; set; }

    public double boltSize { get; set; }
        public double tolerance { get; set; }
        public TeklaPosition position { get; set; }
    public string boltStandard { get; set; }
    public double cutLength { get; set; }
    public List<Point> coordinates { get; set; }
	public List<string> boltedPartsIds { get; set; } = new List<string>(); // First guid is PartToBeBolted, second guid is PartToBoltTo, any others are OtherPartsToBolt

    public Bolts() { }

  }
    public class BoltsXY : Bolts
    {
        // Lists of XY positions of bolts for Tekla
        public List<double> xPosition { get; set; }
        public List<double> yPosition { get; set; }

        public BoltsXY() { }
    }
    public class BoltsArray : Bolts
    {
        // Lists of XY distances between bolts for Tekla
        public List<double> xDistance { get; set; }
        public List<double> yDistance { get; set; }

        public BoltsArray() { }
    }
    public class BoltsCircle : Bolts
    {
        public int boltCount { get; set; }
        public double diameter { get; set; }
        public BoltsCircle() { }
    }
}
