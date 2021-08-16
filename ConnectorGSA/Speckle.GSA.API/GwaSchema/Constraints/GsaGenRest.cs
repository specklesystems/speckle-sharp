using System.Collections.Generic;

namespace Speckle.GSA.API.GwaSchema
{
  public class GsaGenRest : GsaRecord_
  {
    public string Name { get => name; set { name = value; } }
    public RestraintCondition X;
    public RestraintCondition Y;
    public RestraintCondition Z;
    public RestraintCondition XX;
    public RestraintCondition YY;
    public RestraintCondition ZZ;
    public List<int> Node;
    public List<int> Stage;

    public GsaGenRest() : base()
    {
      //Defaults
      Version = 2;
    }
  }
}
