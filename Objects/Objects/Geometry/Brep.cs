using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Geometry
{
  public class Brep : Base, I3DGeometry
  {
    public object rawData { get; set; }
    public string provenance { get; set; }
    public Mesh displayValue { get; set; }

    public string linearUnits { get; set; }
    public Box boundingBox { get; set; }
    public Point center { get; set; }
    public double area { get; set; }
    public double volume { get; set; }
    public Brep()
    {

    }
    public Brep(object rawData, string provenance, Mesh displayValue, string applicationId = null)
    {
      this.rawData = rawData;
      this.provenance = provenance;
      this.displayValue = displayValue;
      this.applicationId = applicationId;
    }

  }
}
