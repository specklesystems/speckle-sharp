using System;
using System.Collections.Generic;
using System.Text;
using Objects.DefaultBuildingObjectKit.Calculations;
using Objects.DefaultBuildingObjectKit.Visualization;
using Speckle.Core.Models;
using Objects.Geometry;

namespace Objects.DefaultBuildingObjectKit.PhysicalObjects
{
  public class BuiltElement1D : Base , IDisplayValue<List<Mesh>> , ISourceAppParams
  {
    public ICurve baseLine { get; set; }

    public string name { get; set; }
    public BuiltElement1DProperty BuiltElement1DProperty { get; set; }
    public Material material { get; set; }
    public List<Base> Voids { get; set; }//Physical objects can have voids in them, non physical objects should not be repersented with voids encompassed

    public Base childrenObject { get; set; } //maybe holding pointers here would make sense more
    public List<Mesh> displayValue { get; set; }
    public string sourceAppName { get ; set ; }
    public List<Base> sourceAppParams { get ; set; }
  }


}
