using System.Collections.Generic;
using Objects.Structural.CSI.Analysis;
using System.Linq;
using Speckle.Core.Kits;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  public List<string> StoriesToNative(CSIStories stories)
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
      storyNames[i] = stories.CSIStory[i].name;
      storyElevations[i] = stories.CSIStory[i].elevation;
      storyHeights[i] = stories.CSIStory[i].storeyHeight;
      isMasterStory[i] = stories.CSIStory[i].IsMasterStory;
      similarToStory[i] = stories.CSIStory[i].SimilarToStory;
      spliceAbove[i] = stories.CSIStory[i].SpliceAbove;
      spliceHeight[i] = stories.CSIStory[i].SpliceHeight;
      colors[i] = stories.CSIStory[i].Color;
    }
    var success = Model.Story.SetStories_2(
      stories.BaseElevation,
      stories.NumberStories,
      ref storyNames,
      ref storyHeights,
      ref isMasterStory,
      ref similarToStory,
      ref spliceAbove,
      ref spliceHeight,
      ref colors
    );

    if (success != 0)
    {
      throw new ConversionException("Failed to set the stories");
    }

    return storyNames.ToList();
  }

  public CSIStories StoriesToSpeckle()
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

    var s = Model.Story.GetStories_2(
      ref baseElevation,
      ref numberOfStories,
      ref names,
      ref storyElevations,
      ref storyHeights,
      ref isMasterStory,
      ref SimilarToStory,
      ref spliceAbove,
      ref spliceHeight,
      ref colors
    );

    var speckleStories = new CSIStories();
    speckleStories.BaseElevation = baseElevation;
    speckleStories.NumberStories = numberOfStories;
    speckleStories.CSIStory = new List<CSIStorey> { };
    for (int index = 0; index < numberOfStories; index++)
    {
      var speckleStory = new CSIStorey(
        names[index],
        storyElevations[index],
        storyHeights[index],
        isMasterStory[index],
        SimilarToStory[index],
        spliceAbove[index],
        spliceHeight[index]
      );
      speckleStories.CSIStory.Add(speckleStory);
    }

    //SpeckleModel.elements.Add(speckleStories);

    return speckleStories;
  }
}
