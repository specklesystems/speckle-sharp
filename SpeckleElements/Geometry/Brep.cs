using Speckle.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Elements.Geometry
{
  public class Brep : Base, IGeometry
  {
    public object rawData { get; set; }
    public string provenance { get; set; }
    public Mesh displayValue { get; set; }
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
