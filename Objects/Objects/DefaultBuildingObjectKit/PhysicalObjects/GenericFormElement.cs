using System;
using System.Collections.Generic;
using System.Text;
using Objects.Geometry;
using Objects.DefaultBuildingObjectKit.Calculations;
using Objects.DefaultBuildingObjectKit.Visualization;
using Speckle.Core.Models;

namespace Objects.DefaultBuildingObjectKit.PhysicalObjects
{
  public abstract class GenericFormElement : Base , IDisplayValue<List<Mesh>> 
  {
    //should we have something similar to BREP definition here but not a Brep Defn ? 
    public List<Mesh> displayValue { get; set; } // for visualization
    //public double surfaceArea { get; set; }// the actual surface area of the 3d element 
    //public string name { get; set; }// for tracking  -> pull down to the lower layers 
    //public double volume { get; set; } //need to have volume on
    public GenericFormProperty Property { get; set; }

    public Material material { get; set; }

    public List<Base> Voids { get; set; }//Physical objects can have voids in them, non physical objects should not be repersented with voids encompassed

    public Base childrenObject { get; set; } //maybe holding pointers here would make sense more

  }
}
