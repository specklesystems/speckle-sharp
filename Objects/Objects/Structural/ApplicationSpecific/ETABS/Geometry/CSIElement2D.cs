using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.CSI.Analysis;
using Objects.Structural.CSI.Geometry;
using Objects.Structural.CSI.Properties;
using Objects.Structural.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.Properties;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.Structural.CSI.Geometry
{
  public class CSIElement2D : Element2D
  {
    [DetachProperty]
    public CSIAreaSpring CSIAreaSpring { get; set; }
    public string DiaphragmAssignment { get; set; }
    public string PierAssignment { get; set; }
    public string SpandrelAssignment { get; set; }
    public double[] modifiers { get; set; }
    public bool Opening { get; set; }

    [SchemaInfo("Element2D", "Creates a Speckle CSI 2D element (based on a list of edge ie. external, geometry defining nodes)", "CSI", "Geometry")]
    public CSIElement2D(List<Node> nodes, Property2D property, double offset = 0, double orientationAngle = 0, double[] modifiers = null, CSIAreaSpring CSIAreaSpring = null, CSIDiaphragm CSIDiaphragm = null)
    {
      topology = nodes;
      this.property = property;
      this.offset = offset;
      this.orientationAngle = orientationAngle;
      DiaphragmAssignment = CSIDiaphragm.name;
      this.CSIAreaSpring = CSIAreaSpring;
      this.modifiers = modifiers;
    }

    public CSIElement2D()
    {
    }
  }
}
