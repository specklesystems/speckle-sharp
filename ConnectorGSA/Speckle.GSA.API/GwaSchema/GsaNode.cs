using System.Collections.Generic;

namespace Speckle.GSA.API.GwaSchema
{
  public class GsaNode : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public Colour Colour = Colour.NO_RGB;
    public double X;
    public double Y;
    public double Z;
    public NodeRestraint NodeRestraint;
    public List<AxisDirection6> Restraints;
    public NodeAxisRefType AxisRefType;
    public int? AxisIndex;
    public double? MeshSize;
    public int? SpringPropertyIndex;
    public int? MassPropertyIndex;
   
    //Damper property is left out at this point

    public GsaNode() : base()
    {
      //Defaults
      Version = 3;
    }

  }
}
