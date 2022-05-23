using System;
using System.Collections.Generic;
using System.Text;
using Objects.DefaultBuildingObjectKit.Calculations;
using Objects.DefaultBuildingObjectKit.Visualization;
using Speckle.Core.Models;
using Objects.Geometry;

namespace Objects.DefaultBuildingObjectKit.PhysicalObjects
{
  public abstract class BuiltElement2D : Base, IDisplayValue<List<Mesh>>
  {
    //Meant for weird funky roofs mainly
    public ICurve baseCurve { get; set; } // projection of outline onto a plane for the non planar elemenets

    public List<Mesh> displayValue { get; set; } // for visualization
    public double area { get; set; }// the actual area of the 2d element 
    //public string name { get; set; }// for tracking 
    public BuiltElement2DProperty BuiltElement2DProperty { get; set; }
    public Material material { get; set; }

    public string sourceAppName { get; set; }
    public List<Base> sourceAppParams { get; set; }
    public List<Base> Voids { get; set; }//Physical objects can have voids in them, non physical objects should not be repersented with voids encompassed -> going down
    public Base childrenObject { get; set; } //maybe holding pointers here would make sense more

  }
}
