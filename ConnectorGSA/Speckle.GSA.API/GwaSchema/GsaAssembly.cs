using System.Collections.Generic;

namespace Speckle.GSA.API.GwaSchema
{
  public partial class GsaAssembly : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public GSAEntity Type;
    public List<int> ElementIndices = new List<int>();
    public List<int> MemberIndices = new List<int>();
    public int? Topo1;
    public int? Topo2;
    public int? OrientNode;
    public List<int> IntTopo = new List<int>();
    public double SizeY;
    public double SizeZ;
    public CurveType CurveType;
    public int? CurveOrder;
    public PointDefinition PointDefn;
    //Only one of these will be set, according to the PointDefn value
    public int? NumberOfPoints;
    public double? Spacing;
    public List<int> StoreyIndices = new List<int>();
    public List<double> ExplicitPositions = new List<double>();

    public GsaAssembly() : base()
    {
      Version = 3;
    }
  }
}
