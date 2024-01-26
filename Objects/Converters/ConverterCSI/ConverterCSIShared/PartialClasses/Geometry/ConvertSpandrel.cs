using Objects.Structural.CSI.Geometry;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  public void SpandrelToNative(CSISpandrel spandrel)
  {
    Model.SpandrelLabel.SetSpandrel(spandrel.name, spandrel.multistory);
  }

  public CSISpandrel SpandrelToSpeckle(string name)
  {
    int numberStories = 0;
    string[] storyName = null;
    int[] numAreaObjs = null;
    int[] numLineObjs = null;
    double[] length = null;
    double[] depthLeft = null;
    double[] thickLeft = null;
    double[] depthRight = null;
    double[] thickRight = null;
    string[] matProp = null;
    double[] centerofGravityLeftX = null;
    double[] centerofGravityLeftY = null;
    double[] centerofGravityLeftZ = null;
    double[] centerofGravityRightX = null;
    double[] centerofGravityRightY = null;
    double[] centerofGravityRightZ = null;

    Model.SpandrelLabel.GetSectionProperties(
      name,
      ref numberStories,
      ref storyName,
      ref numAreaObjs,
      ref numLineObjs,
      ref length,
      ref depthLeft,
      ref thickLeft,
      ref depthRight,
      ref thickRight,
      ref matProp,
      ref centerofGravityLeftX,
      ref centerofGravityLeftY,
      ref centerofGravityLeftZ,
      ref centerofGravityRightX,
      ref centerofGravityRightY,
      ref centerofGravityRightZ
    );
    bool multistory = false;
    Model.SpandrelLabel.GetSpandrel(name, ref multistory);

    var speckleCSISpandrel = new CSISpandrel(
      name,
      multistory,
      numberStories,
      storyName,
      numAreaObjs,
      numLineObjs,
      length,
      depthLeft,
      thickLeft,
      depthRight,
      thickRight,
      matProp,
      centerofGravityLeftX,
      centerofGravityLeftY,
      centerofGravityLeftZ,
      centerofGravityRightX,
      centerofGravityRightY,
      centerofGravityRightZ
    );
    SpeckleModel.elements.Add(speckleCSISpandrel);
    return speckleCSISpandrel;
  }
}
