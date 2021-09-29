namespace Speckle.GSA.API.GwaSchema
{
  //polygon references not supported yet
  public class GsaLoadGridArea : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public int? GridSurfaceIndex;
    public LoadAreaOption Area;
    public int? PolygonIndex;
    public string Polygon;
    public int? LoadCaseIndex;
    public AxisRefType AxisRefType;
    public int? AxisIndex;
    public bool Projected;
    public AxisDirection3 LoadDirection;
    public double? Value;

    public GsaLoadGridArea() : base()
    {
      //Defaults
      Version = 2;
    }
  }
}
