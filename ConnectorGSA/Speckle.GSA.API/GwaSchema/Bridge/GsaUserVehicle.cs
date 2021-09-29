using System.Collections.Generic;

namespace Speckle.GSA.API.GwaSchema
{
  public class GsaUserVehicle : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public double? Width;
    public int? NumAxle;
    public List<double> AxlePosition;
    public List<double> AxleOffset;
    public List<double> AxleLeft;
    public List<double> AxleRight;

    public GsaUserVehicle() : base()
    {
      //Defaults
      Version = 1;
    }
  }
}
