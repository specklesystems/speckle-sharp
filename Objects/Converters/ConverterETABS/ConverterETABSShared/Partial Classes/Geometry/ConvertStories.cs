using System;
using System.Collections.Generic;
using System.Text;
using ETABSv1;
using Objects.Structural.Properties;
using Objects.Structural.Materials;
using Objects.Structural.ETABS.Analysis;

namespace Objects.Converter.ETABS
{
    public partial class ConverterETABS
    {
        public ETABSStories StoriesToSpeckle()
        {
            double baseElevation = 0;
            int[] colors = null;
            string[] names = null;
            double[] storyElevations = null;
            double[] storyHeights = null;
            bool[] isMasterStory = null;
            string[] SimilarToStory = null;
            bool[] spliceAbove = null;
            double[] spliceHeight = null;
            int numberOfStories = 0;

            Model.Story.GetStories_2(ref baseElevation, ref numberOfStories,ref names, ref storyElevations, ref storyHeights, ref isMasterStory, ref SimilarToStory, ref spliceAbove, ref spliceHeight, ref colors);

            var speckleStories = new ETABSStories();


            return speckleStories; 
        }
    }
}
