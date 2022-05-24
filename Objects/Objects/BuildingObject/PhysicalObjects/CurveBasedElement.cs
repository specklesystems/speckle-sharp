using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuildingObject.Calculations;
using Speckle.Core.Models;
using Objects.Geometry;
using Objects.Visualization;

namespace Objects.BuildingObject.PhysicalObjects
{
  public abstract class CurveBasedElement : Base, IDisplayValue<List<Mesh>>
  {
    public List<ICurve> baseCurve { get; set; } // lenght is included  can have multiple of ICurves

    public List<Mesh> displayValue { get; set; } // for visualization
    //public double area { get; set; }// the actual area of the 2d element 
    //public string name { get; set; }// for tracking 
    public BaseCurveProperty Property { get; set; }
    public Material material { get; set; }

    public List<Base> Voids { get; set; }//Physical objects can have voids in them, non physical objects should not be repersented with voids encompassed -> going down
    public Base childrenObject { get; set; } //maybe holding pointers here would make sense more

  }
}
