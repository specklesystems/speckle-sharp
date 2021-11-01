namespace Speckle.GSA.API.GwaSchema
{
  public class GsaUnitData : GsaRecord
  {
    public UnitDimension Option;
    public string Name { get => name; set { name = value; } }
    public double Factor;

    public GsaUnitData()
    {
      Version = 1;
    }
  }
}
