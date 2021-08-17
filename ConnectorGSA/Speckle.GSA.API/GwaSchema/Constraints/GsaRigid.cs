using System.Collections.Generic;

namespace Speckle.GSA.API.GwaSchema
{
  //polygon references not supported yet
  public class GsaRigid : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public int? PrimaryNode;
    public RigidConstraintType Type;
    public Dictionary<AxisDirection6,List<AxisDirection6>> Link;
    public List<int> ConstrainedNodes;
    public List<int> Stage;
    public int? ParentMember;

    public GsaRigid() : base()
    {
      //Defaults
      Version = 3;
    }
  }
}
