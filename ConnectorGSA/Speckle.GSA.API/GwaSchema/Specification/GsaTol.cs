namespace Speckle.GSA.API.GwaSchema
{
  public class GsaTol : GsaRecord
  {
    //TOL | vertical | node | grid_edge | grid_angle | spacer_leg | memb_angle | memb_edge | refinement | elem_plane | member_curve
    public double VerticalDegrees;  //Added the degrees to the name to reflect the fact that this value is after conversion from tan(degrees) in the GWA
    public double Node;
    public double GridEdge;
    public double GridAngle;
    public double SpacerLeg;
    public double MembAngle;
    public double MembEdge;
    public double Refinement;
    public double ElemPlaneDegrees; //Added the degrees to the name to reflect the fact that this value is after conversion from tan(degrees) in the GWA
    public double MemberCurve;

    public GsaTol()
    {
      Version = 1;
    }
  }
}
