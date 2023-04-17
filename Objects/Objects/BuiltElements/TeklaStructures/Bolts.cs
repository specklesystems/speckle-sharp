using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements.TeklaStructures;

public class Bolts : Base
{
  [DetachProperty]
  public List<Mesh> displayValue { get; set; }

  public Point firstPosition { get; set; }
  public Point secondPosition { get; set; }

  public double length { get; set; }
  public double boltSize { get; set; }
  public double tolerance { get; set; }
  public TeklaPosition position { get; set; }
  public string boltStandard { get; set; }
  public double cutLength { get; set; }
  public List<Point> coordinates { get; set; }
  public List<string> boltedPartsIds { get; set; } = new(); // First guid is PartToBeBolted, second guid is PartToBoltTo, any others are OtherPartsToBolt
}

public class BoltsXY : Bolts
{
  // Lists of XY positions of bolts for Tekla
  public List<double> xPosition { get; set; }
  public List<double> yPosition { get; set; }
}

public class BoltsArray : Bolts
{
  // Lists of XY distances between bolts for Tekla
  public List<double> xDistance { get; set; }
  public List<double> yDistance { get; set; }
}

public class BoltsCircle : Bolts
{
  public int boltCount { get; set; }
  public double diameter { get; set; }
}
