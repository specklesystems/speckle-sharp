using System.Collections.Generic;

namespace Speckle.GSA.API.GwaSchema
{
  public class GsaAnalStage : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public Colour Colour;
    public List<int> List;
    public double? Phi;
    public int? Days;
    public List<int> Lock;

    public GsaAnalStage()
    {
      Version = 3;
    }
  }
}
