using Objects.Primitive;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Objects.Geometry
{

  //TODO: to finish
  public class Surface : Base, IGeometry
  {
    public int degreeU { get; set; }
    public int degreeV { get; set; }
    public bool rational { get; set; } 
    public List<List<ControlPoint>> points { get; set; }
    public List<double> knotsU { get; set; }
    public List<double> knotsV { get; set; }
    public Interval domainU { get; set; }
    public Interval domainV { get; set; }
    public Mesh displayValue { get; set; }
    public bool closedU { get; set; }
    public bool closedV { get; set; }

    public Surface(Mesh poly, string applicationId = null)
    {
      this.displayValue = poly;
      this.applicationId = applicationId;
    }
  }
}
