namespace Speckle.GSA.API.GwaSchema
{
  public class GsaProp2d : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public Colour Colour = Colour.NO_RGB;
    public Property2dType Type;
    public AxisRefType AxisRefType;
    public int? AxisIndex;
    public int? AnalysisMaterialIndex;
    public Property2dMaterialType MatType;
    public int? GradeIndex;
    public int? DesignIndex;
    public double? Thickness;
    public string Profile = ""; //Not supported yet
    public Property2dRefSurface RefPt;
    public double RefZ;
    public double Mass;
    //For each of these next 4 pairs, only one will be filled per pair, depending on the presence or absense of the % sign
    public double? BendingStiffnessPercentage;
    public double? Bending;
    public double? ShearStiffnessPercentage;
    public double? Shear;
    public double? InPlaneStiffnessPercentage;
    public double? InPlane;
    public double? VolumePercentage;
    public double? Volume;

    public GsaProp2d() : base()
    {
      //Defaults
      Version = 7;
    }
  }
}
