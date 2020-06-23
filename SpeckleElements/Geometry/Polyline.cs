using Speckle.Elements.Primitive;
using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Speckle.Elements.Geometry
{
  public class Polyline : Geometry
  {
    public List<double> value { get; set; }
    public bool closed { get; set; }
    public Interval domain { get; set; }
    public Polyline()
    {

    }
    public Polyline(IEnumerable<double> coordinatesArray, string applicationId = null)
    {
      this.value = coordinatesArray.ToList();
      this.applicationId = applicationId;
    }
  }
}
