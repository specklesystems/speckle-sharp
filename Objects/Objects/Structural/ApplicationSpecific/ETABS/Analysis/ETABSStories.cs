using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;

namespace Objects.Structural.ETABS.Analysis
{
    public class ETABSStories : Base
    {
        public double BaseElevation { get; set;}
        public int NumberStories { get; set; }
        public string[] Names { get; set; }
        public double[] StoryElevations { get; set; }
        public double[] StoryHeights { get; set; }
        public bool[] IsMasterStory { get; set; }
        public string[] SimilarToStory { get; set; }
        public bool[] SpliceAbove { get; set; }
        public double[] SpliceHeight { get; set; }
        public int[] Color { get; set; }

        public ETABSStories() { }
        public ETABSStories(double baseElevation, int numberStories, string[] names, double[] storyElevations, double[] storyHeights, bool[] isMasterStory, string[] similarToStory, bool[] spliceAbove, double[] spliceHeight, int[] color)
        {
            BaseElevation = baseElevation;
            NumberStories = numberStories;
            Names = names;
            StoryElevations = storyElevations;
            StoryHeights = storyHeights;
            IsMasterStory = isMasterStory;
            SimilarToStory = similarToStory;
            SpliceAbove = spliceAbove;
            SpliceHeight = spliceHeight;
            Color = color;
        }
    }
}
