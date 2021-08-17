namespace Speckle.GSA.API.GwaSchema
{
  //polygon references not supported yet
  public class GsaLoadGridPoint : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public int? GridSurfaceIndex;
    public double? X;
    public double? Y;
    public int? LoadCaseIndex;
    public AxisRefType AxisRefType;
    public int? AxisIndex;
    public AxisDirection3 LoadDirection;
    public double? Value;

    public GsaLoadGridPoint() : base()
    {
      //Defaults
      Version = 2;
    }
  }
}
