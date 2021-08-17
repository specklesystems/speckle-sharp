namespace Speckle.GSA.API.GwaSchema
{
  public class GsaGridLine : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public GridLineType Type;
    public double? XCoordinate;
    public double? YCoordinate;
    public double? Length;
    public double? Theta1;
    public double? Theta2;

    public GsaGridLine() : base()
    {
      //Defaults
      Version = 1;
    }
  }
}
