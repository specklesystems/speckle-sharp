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

      var s =  Model.Story.GetStories_2(ref baseElevation, ref numberOfStories, ref names, ref storyElevations, ref storyHeights, ref isMasterStory, ref SimilarToStory, ref spliceAbove, ref spliceHeight, ref colors);

      var speckleStories = new ETABSStories();
      speckleStories.BaseElevation = baseElevation;
      speckleStories.NumberStories = numberOfStories;
      speckleStories.etabsStories = new List<ETABSStorey> { };
      for(int index = 0; index<numberOfStories;index ++){
        var speckleStory = new ETABSStorey(storyHeights[index], isMasterStory[index], SimilarToStory[index], spliceAbove[index], spliceHeight[index]);
        speckleStories.etabsStories.Add(speckleStory);
      }

      //SpeckleModel.elements.Add(speckleStories);

      return speckleStories;
    }
  }
}
