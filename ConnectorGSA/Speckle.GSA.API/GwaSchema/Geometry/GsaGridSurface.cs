using System.Collections.Generic;

namespace Speckle.GSA.API.GwaSchema
{
  public class GsaGridSurface : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public GridPlaneAxisRefType PlaneRefType;
    public int? PlaneIndex;
    public GridSurfaceElementsType Type;
    //public bool AllIndices = false;
    public List<int> ElementIndices = new List<int>();
    public List<int> MemberIndices = new List<int>();
    public double? Tolerance;
    public GridSurfaceSpan Span;
    public double? Angle;  //Degrees
    public GridExpansion Expansion;

    public GsaGridSurface() : base()
    {
      //Defaults
      Version = 1;
    }
  }
}
