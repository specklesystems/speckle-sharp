using System.Collections.Generic;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Revit;

public class RevitStair : Base, IDisplayValue<List<Mesh>>
{
  public string family { get; set; }
  public string type { get; set; }
  public Level level { get; set; }
  public Level topLevel { get; set; }
  public double riserHeight { get; set; }
  public int risersNumber { get; set; }
  public double treadDepth { get; set; }
  public int treadsNumber { get; set; }
  public double baseElevation { get; set; }
  public double topElevation { get; set; }
  public bool beginsWithRiser { get; set; }
  public double height { get; set; }
  public int numberOfStories { get; set; }
  public Base parameters { get; set; }
  public List<RevitStairRun> runs { get; set; }
  public List<RevitStairLanding> landings { get; set; }
  public List<RevitStairSupport> supports { get; set; }
  public string elementId { get; set; }

  public string units { get; set; }

  [DetachProperty]
  public List<Mesh> displayValue { get; set; }
}

public class RevitStairRun : Base
{
  public string family { get; set; }
  public string type { get; set; }
  public Polycurve path { get; set; }
  public Polycurve outline { get; set; }
  public double runWidth { get; set; }
  public int risersNumber { get; set; }
  public int treadsNumber { get; set; }
  public double baseElevation { get; set; }
  public double topElevation { get; set; }
  public bool beginsWithRiser { get; set; }
  public bool endsWithRiser { get; set; }
  public double extensionBelowRiserBase { get; set; }
  public double extensionBelowTreadBase { get; set; }
  public double height { get; set; }
  public string runStyle { get; set; }
  public Base parameters { get; set; }
  public string elementId { get; set; }

  public string units { get; set; }
}

public class RevitStairLanding : Base
{
  public string family { get; set; }
  public string type { get; set; }
  public bool isAutomaticLanding { get; set; }
  public double baseElevation { get; set; }
  public double thickness { get; set; }
  public Polycurve outline { get; set; }
  public Base parameters { get; set; }
  public string elementId { get; set; }

  public string units { get; set; }
}

public class RevitStairSupport : Base
{
  public string family { get; set; }
  public string type { get; set; }
  public Base parameters { get; set; }
  public string elementId { get; set; }

  public string units { get; set; }
}
