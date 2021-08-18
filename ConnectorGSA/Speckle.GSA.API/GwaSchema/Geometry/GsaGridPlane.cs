namespace Speckle.GSA.API.GwaSchema
{
  public class GsaGridPlane : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public GridPlaneType Type;
    public GridPlaneAxisRefType AxisRefType;
    public int? AxisIndex;
    public double? Elevation;
    public bool StoreyToleranceBelowAuto;
    public double? StoreyToleranceBelow;
    public bool StoreyToleranceAboveAuto;
    public double? StoreyToleranceAbove;

    public GsaGridPlane() : base()
    {
      //Defaults
      Version = 4;
    }
  }
}
