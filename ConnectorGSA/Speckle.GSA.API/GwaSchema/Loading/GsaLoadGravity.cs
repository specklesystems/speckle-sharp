using System.Collections.Generic;

namespace Speckle.GSA.API.GwaSchema
{
  public class GsaLoadGravity : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public List<int> ElementIndices = new List<int>();
    public List<int> MemberIndices = new List<int>();
    public List<int> Nodes = new List<int>();
    public int? LoadCaseIndex;
    public double? X;
    public double? Y;
    public double? Z;

    public GsaLoadGravity()
    {
      Version = 3;
    }
  }
}
