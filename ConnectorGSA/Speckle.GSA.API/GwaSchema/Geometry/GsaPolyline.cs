using System.Collections.Generic;

namespace Speckle.GSA.API.GwaSchema
{
  public class GsaPolyline : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public Colour Colour;
    public int? GridPlaneIndex;
    public int NumDim;
    public List<double> Values;
    public string Units;

    public GsaPolyline() : base()
    {
      //Defaults
      Version = 1;
    }
  }
}
