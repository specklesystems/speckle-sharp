namespace Speckle.GSA.API.GwaSchema
{
  public class GsaMatSteel : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public GsaMat Mat;
    public double? Fy;
    public double? Fu;
    public double? EpsP;
    public double? Eh;

    public GsaMatSteel() : base()
    {
      //Defaults
      Version = 3;
    }
  }
}

