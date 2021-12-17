using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Properties;
using Objects.Structural.ETABS.Analysis;
using Objects.Structural.ETABS.Geometry;
using Objects.Structural.ETABS.Properties;
using Objects.Structural.Materials;

namespace Objects.Structural.ETABS.Geometry
{
  public class ETABSElement2D : Element2D
  {
    [DetachProperty]
    public  ETABSAreaSpring ETABSAreaSpring { get; set; }
    public string PierAssignment { get; set; }
    public string SpandrelAssignment { get; set; }
    public double[] modifiers { get; set; }

    [SchemaInfo("Element2D", "Creates a Speckle structural 2D element (based on a list of edge ie. external, geometry defining nodes)", "Structural", "Geometry")]
    public ETABSElement2D(List<Node> nodes, Property2D property, double offset = 0, double orientationAngle = 0, ETABSAreaSpring ETABSAreaSpring = null)
    {
      this.topology = nodes;
      this.property = property;
      this.offset = offset;
      this.orientationAngle = orientationAngle;
      this.ETABSAreaSpring = ETABSAreaSpring;
    }

    public ETABSElement2D()
    {
    }
  }
}
