using System.Collections.Generic;

namespace Speckle.GSA.API.GwaSchema
{
  public class GsaAnalStage : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public Colour Colour;
    public List<int> ElementIndices = new List<int>();
    public List<int> MemberIndices = new List<int>();
    public double? Phi;
    public int? Days;
    public List<int> LockMemberIndices;
    public List<int> LockElementIndices;

    public GsaAnalStage()
    {
      Version = 3;
    }
  }
}
