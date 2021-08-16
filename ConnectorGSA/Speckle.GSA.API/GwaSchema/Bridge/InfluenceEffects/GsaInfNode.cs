namespace Speckle.GSA.API.GwaSchema
{
  public class GsaInfNode : GsaRecord_
  {
    public string Name { get => name; set { name = value; } }
    public int? Action;
    public int? Node;
    public double? Factor;
    public InfType Type;
    public AxisRefType AxisRefType;
    public AxisDirection6 Direction;

    public GsaInfNode() : base()
    {
      //Defaults
      Version = 1;
    }
  }
}
