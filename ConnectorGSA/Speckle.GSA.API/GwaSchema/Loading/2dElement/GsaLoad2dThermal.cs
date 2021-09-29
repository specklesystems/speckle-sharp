using System.Collections.Generic;

namespace Speckle.GSA.API.GwaSchema
{
  public class GsaLoad2dThermal : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public List<int> ElementIndices; 
    public List<int> MemberIndices;
    public int? LoadCaseIndex;
    public Load2dThermalType Type;
    public List<double> Values;

    public GsaLoad2dThermal() : base()
    {
      //Defaults
      Version = 2;
    }
  }
}
