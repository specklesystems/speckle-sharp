using Objects.Structural.CSI.Geometry;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  public void PierToNative(CSIPier CSIPier)
  {
    Model.PierLabel.SetPier(CSIPier.name);
  }

  public CSIPier PierToSpeckle(string name)
  {
    int numberStories = 0;
    string[] storyName = null;
    int[] numAreaObjs = null;
    int[] numLineObjs = null;
    double[] axisAngle = null;
    double[] widthBot = null;
    double[] thicknessBot = null;
    double[] widthTop = null;
    double[] thicknessTop = null;
    string[] matProp = null;
    double[] centerofGravityBotX = null;
    double[] centerofGravityBotY = null;
    double[] centerofGravityBotZ = null;
    double[] centerofGravityTopX = null;
    double[] centerofGravityTopY = null;
    double[] centerofGravityTopZ = null;

    // **** ISSUE WITH THIS METHOD HANGING IF PIER LABEL NOT ASSIGNED IN MODEL **** //
    //var s = Model.PierLabel.GetSectionProperties(name, ref numberStories, ref storyName, ref axisAngle, ref numAreaObjs, ref numLineObjs, ref widthBot, ref thicknessBot, ref widthTop, ref thicknessTop, ref matProp
    //, ref centerofGravityBotX, ref centerofGravityBotY, ref centerofGravityBotZ, ref centerofGravityTopX, ref centerofGravityTopY, ref centerofGravityTopZ);

    //var speckleCSIPier = new CSIPier(name, numberStories, storyName, axisAngle, numAreaObjs, numLineObjs, widthBot, thicknessBot, widthTop, thicknessTop, matProp, centerofGravityBotX, centerofGravityTopY, centerofGravityTopZ, centerofGravityTopX, centerofGravityBotY, centerofGravityBotZ);
    var speckleCSIPier = new CSIPier();
    speckleCSIPier.name = name;
    SpeckleModel.elements.Add(speckleCSIPier);
    return speckleCSIPier;
  }
}
