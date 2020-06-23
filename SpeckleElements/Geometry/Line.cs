using Speckle.Elements.Primitive;
using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Speckle.Elements.Geometry
{
  public class Line : Geometry
  {
    public List<double> value { get; set; }
    public Interval domain { get; set; }

    public Line() { }

    public Line(double x, double y, double z = 0, string applicationId = null)
    {
      this.value = new List<double>() { x, y, z };
      this.applicationId = applicationId;
    }

    public Line(IEnumerable<double> coordinatesArray, string applicationId = null)
    {
      this.value = coordinatesArray.ToList();
      this.applicationId = applicationId;
    }
  }
}
