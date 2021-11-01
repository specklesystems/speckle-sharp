using System.Collections.Generic;

namespace Speckle.GSA.API.GwaSchema
{
  public class GsaLoadNode : GsaRecord
  {
    //As many of these should be nullable, or in the case of enums, a "NotSet" option, to facilitate merging objects received from Speckle 
    //with existing objects in the GSA model
    public string Name { get => name; set { name = value; } }
    public List<int> NodeIndices = new List<int>();
    public int? LoadCaseIndex;
    public bool GlobalAxis;
    public int? AxisIndex;
    public AxisDirection6 LoadDirection;
    public double? Value;

    public GsaLoadNode(): base()
    {
      //Defaults
      Version = 2;
    }
  }
}
