using System.Collections.Generic;

namespace Speckle.GSA.API.GwaSchema
{
  public class GsaLoad2dFace : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public List<int> ElementIndices;
    public List<int> MemberIndices;
    public int? LoadCaseIndex;
    public AxisRefType AxisRefType;
    public int? AxisIndex;
    public Load2dFaceType Type;
    public bool Projected;
    public AxisDirection3 LoadDirection;
    public List<double> Values;
    public double? R;
    public double? S;

    public GsaLoad2dFace() : base()
    {
      //Defaults
      Version = 2;
    }
  }
}
