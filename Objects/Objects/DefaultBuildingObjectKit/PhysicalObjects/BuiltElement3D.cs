using System;
using System.Collections.Generic;
using System.Text;
using Objects.Geometry;
using Objects.DefaultBuildingObjectKit.Calculations;
using Objects.DefaultBuildingObjectKit.Visualization;
using Speckle.Core.Models;

namespace Objects.DefaultBuildingObjectKit.PhysicalObjects
{
  public class BuiltElement3D : Base , IDisplayValue<List<Mesh>> , ISourceAppParams
  {
    //should we have something similar to BREP definition here but not a Brep Defn ? 
    public List<Mesh> displayValue { get; set; } // for visualization
    public double surfaceArea { get; set; }// the actual surface area of the 3d element 
    public string name { get; set; }// for tracking 
    public double volume { get; set; } //need to have volume on
    public BuiltElement3DProperty BuiltElement3DProperty { get; set; }

    public Material material { get; set; }

    public List<Base> Voids { get; set; }//Physical objects can have voids in them, non physical objects should not be repersented with voids encompassed

    public Base childrenObject { get; set; } //maybe holding pointers here would make sense more

    public string sourceAppName { get; set; }
    public List<Base> sourceAppParams { get; set; }
  }
}
