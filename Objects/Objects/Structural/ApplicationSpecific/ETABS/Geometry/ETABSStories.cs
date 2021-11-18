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

        public List<ETABSStorey> etabsStories {get;set;}

        public ETABSStories() { }


    }
    public class ETABSStorey: Storey
    {
       public double storeyHeight { get; set; }
       public bool IsMasterStory { get; set; }
       public string SimilarToStory { get; set; }
       public bool SpliceAbove { get; set; }
        
        public double SpliceHeight { get; set; }
        public double Color { get; set; }
    }
}
