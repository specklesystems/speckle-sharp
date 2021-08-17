namespace Speckle.GSA.API.GwaSchema
{
  public class GsaPath : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public PathType Type;
    public int? Group;
    public int? Alignment;
    public double? Left;
    public double? Right;
    public double? Factor;
    public int? NumMarkedLanes;

    public GsaPath() : base()
    {
      //Defaults
      Version = 1;
    }
  }
}
