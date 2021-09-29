using System.Collections.Generic;

namespace Speckle.GSA.API.GwaSchema
{
  public class GsaAlign : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public int? GridSurfaceIndex;
    public int? NumAlignmentPoints;
    public List<double> Chain;
    public List<double> Curv;

    public GsaAlign() : base()
    {
      //Defaults
      Version = 1;
    }
  }
}
