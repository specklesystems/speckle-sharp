using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Properties;

namespace Objects.Structural.GSA.Geometry
{
  public class GSAMember2D : Element2D
  {
    public int nativeId { get; set; }
    public int group { get; set; }
    public string colour { get; set; }
    public bool isDummy { get; set; }
    public bool intersectsWithOthers { get; set; }
    public double targetMeshSize { get; set; }
    public List<List<Node>> voids { get; set; }

    public GSAMember2D() { }

    [SchemaInfo("GSAMember2D", "Creates a Speckle structural 2D member for GSA", "GSA", "Geometry")]
    public GSAMember2D([SchemaParamInfo("An ordered list of nodes which represents the perimeter of a member (ie. order of points should based on valid polyline)")] List<Structural.Geometry.Node> perimeter,
        Property2D property, ElementType2D type,
        [SchemaParamInfo("A list of ordered lists of nodes representing the voids within a member (ie. order of points should be based on valid polyline)")] List<List<Structural.Geometry.Node>> voids = null,
        double offset = 0, double orientationAngle = 0)
    {
      this.topology = perimeter; //needs to be ordered properly (ie. matching the point order of a valid polyline)            
      this.property = property;
      this.type = type;
      this.voids = voids; //needs to be ordered properly (ie. matching the point order of a valid polyline)
      this.offset = offset;
      this.orientationAngle = orientationAngle;
    }
  }
}
