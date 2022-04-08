using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Objects.Structural.Geometry;
using System;
using System.Collections.Generic;

namespace Objects.Structural.ETABS.Analysis
{
  public class ETABSStories : Base
  {
    public double BaseElevation { get; set; }
    public int NumberStories { get; set; }

    [DetachProperty]
    public List<ETABSStorey> etabsStories { get; set; }

    public ETABSStories() { }


  }
  public class ETABSStorey : Storey
  {
    public double storeyHeight { get; set; }
    public bool IsMasterStory { get; set; }
    public string SimilarToStory { get; set; }
    public bool SpliceAbove { get; set; }

    public double SpliceHeight { get; set; }
    public int Color { get; set; }

    public ETABSStorey(string name,double elevation,double storeyHeight, bool isMasterStory, string similarToStory, bool spliceAbove, double spliceHeight)
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

    public ETABSStorey()
    {
    }
  }
}
