using System.Collections.Generic;

namespace Speckle.GSA.API.GwaSchema
{
  public class GsaPropSpr : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public Colour Colour = Colour.NO_RGB;
    public StructuralSpringPropertyType PropertyType;
    public Dictionary<AxisDirection6, double> Stiffnesses;
    public double? FrictionCoeff;
    public double? DampingRatio;
    //For GENERAL, there is the option of non-linear curves, but this isn't supported yet
    //Also for LOCKUP, there are positive and negative parameters, but these aren't supported yet either

    public GsaPropSpr() : base()
    {
      //Defaults
      Version = 4;
    }
  }
}
