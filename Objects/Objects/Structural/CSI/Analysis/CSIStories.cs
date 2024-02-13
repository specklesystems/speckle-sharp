using System.Collections.Generic;
using Objects.Structural.Geometry;
using Speckle.Core.Models;

namespace Objects.Structural.CSI.Analysis;

public class CSIStories : Base
{
  public double BaseElevation { get; set; }
  public int NumberStories { get; set; }

  [DetachProperty]
  public List<CSIStorey> CSIStory { get; set; }
}

public class CSIStorey : Storey
{
  public CSIStorey(
    string name,
    double elevation,
    double storeyHeight,
    bool isMasterStory,
    string similarToStory,
    bool spliceAbove,
    double spliceHeight
  )
  {
    this.name = name;
    this.elevation = elevation;
    this.storeyHeight = storeyHeight;
    IsMasterStory = isMasterStory;
    SimilarToStory = similarToStory;
    SpliceAbove = spliceAbove;
    SpliceHeight = spliceHeight;
    Color = 0;
  }

  public CSIStorey() { }

  public double storeyHeight { get; set; }
  public bool IsMasterStory { get; set; }
  public string SimilarToStory { get; set; }
  public bool SpliceAbove { get; set; }

  public double SpliceHeight { get; set; }
  public int Color { get; set; }
}
