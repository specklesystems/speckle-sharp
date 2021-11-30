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
    public void StoriesToNative(ETABSStories stories)
    {
      string[] storyNames = new string[stories.NumberStories];
      double[] storyElevations = new double[stories.NumberStories];
      double[] storyHeights = new double[stories.NumberStories];
      bool[] isMasterStory = new bool[stories.NumberStories];
      string[] similarToStory = new string[stories.NumberStories];
      bool[] spliceAbove = new bool[stories.NumberStories];
      double[] spliceHeight = new double[stories.NumberStories];
      int[] colors = new int[stories.NumberStories];

      for (int i = 0; i < stories.NumberStories; i++)
      {
        storyNames[i] = stories.etabsStories[i].name;
        storyElevations[i] = stories.etabsStories[i].elevation;
        storyHeights[i] = stories.etabsStories[i].storeyHeight;
        isMasterStory[i] = stories.etabsStories[i].IsMasterStory;
        similarToStory[i] = stories.etabsStories[i].SimilarToStory;
        spliceAbove[i] = stories.etabsStories[i].SpliceAbove;
        spliceHeight[i] = stories.etabsStories[i].SpliceHeight;
        colors[i] = stories.etabsStories[i].Color;

      }
      Model.Story.SetStories_2(stories.BaseElevation, stories.NumberStories,ref  storyNames,ref storyHeights,ref isMasterStory,ref similarToStory,ref spliceAbove,ref spliceHeight,ref colors);
    }
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

      var s = Model.Story.GetStories_2(ref baseElevation, ref numberOfStories, ref names, ref storyElevations, ref storyHeights, ref isMasterStory, ref SimilarToStory, ref spliceAbove, ref spliceHeight, ref colors);

      var speckleStories = new ETABSStories();
      speckleStories.BaseElevation = baseElevation;
      speckleStories.NumberStories = numberOfStories;
      speckleStories.etabsStories = new List<ETABSStorey> { };
      for (int index = 0; index < numberOfStories; index++)
      {
        var speckleStory = new ETABSStorey(names[index],storyElevations[index],storyHeights[index], isMasterStory[index], SimilarToStory[index], spliceAbove[index], spliceHeight[index]);
        speckleStories.etabsStories.Add(speckleStory);
      }
      
      //SpeckleModel.elements.Add(speckleStories);

      return speckleStories;
    }
  }
}
