namespace Speckle.GSA.API.GwaSchema
{
  //polygon references not supported yet
  public class GsaLoadGridLine : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public int? GridSurfaceIndex;
    public LoadLineOption Line;
    public int? PolygonIndex;
    public string Polygon;
    public int? LoadCaseIndex;
    public AxisRefType AxisRefType;
    public int? AxisIndex;
    public bool Projected;
    public AxisDirection3 LoadDirection;
    public double? Value1;
    public double? Value2;

    public GsaLoadGridLine() : base()
    {
      //Defaults
      Version = 2;
    }
  }
}
