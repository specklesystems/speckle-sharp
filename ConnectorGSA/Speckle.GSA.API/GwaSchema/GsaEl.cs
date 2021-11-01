using System.Collections.Generic;

namespace Speckle.GSA.API.GwaSchema
{
  public class GsaEl : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public Colour Colour = Colour.NO_RGB;
    public ElementType Type;
    public int? PropertyIndex;
    //The taper offsets aren't mentioned in the documentation but they are there encapsulated in square brackets in he property field
    public double? TaperOffsetPercentageEnd1;
    public double? TaperOffsetPercentageEnd2;
    public int? Group;
    public List<int> NodeIndices;           //Perimeter/edge topology, the number of which depends on int value for the ElementType value
    public int? OrientationNodeIndex;
    public double? Angle;  //Degrees - GWA also stores this in degrees
    public ReleaseInclusion ReleaseInclusion;
    public Dictionary<AxisDirection6, ReleaseCode> Releases1;
    public List<double> Stiffnesses1;
    public Dictionary<AxisDirection6, ReleaseCode> Releases2;
    public List<double> Stiffnesses2;
    public double? End1OffsetX;
    public double? End2OffsetX;
    public double? OffsetY;
    public double? OffsetZ;
    public bool Dummy;
    public int? ParentIndex;

    public GsaEl() : base()
    {
      Version = 4;
    }
  }
}
