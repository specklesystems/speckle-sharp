namespace Speckle.GSA.API.GwaSchema
{
  public class GsaMatCurve : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public Dimension Abscissa;
    public Dimension Ordinate;
    public double [,] Table;

    public GsaMatCurve() : base()
    {
      //Defaults
      Version = 1;
    }
  }
}
