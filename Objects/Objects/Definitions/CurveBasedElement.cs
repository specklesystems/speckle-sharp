using System;
using System.Collections.Generic;
using System.Text;
using Objects.Definitions;
using Speckle.Core.Models;
using Objects.Geometry;
using Objects.Visualization;

namespace Objects.Definitions
{
  public abstract class CurveBasedElement : Base, IDisplayValue<List<Mesh>>
  {
    public ICurve baseCurve { get; set; }

    public List<Mesh> displayValue { get; set; } // for visualization
    //public double area { get; set; }// the actual area of the 2d element 
    //public string name { get; set; }// for tracking 
    public BaseCurveProperty Property { get; set; }
    public Material material { get; set; }

    public List<Base> Voids { get; set; }//Physical objects can have voids in them, non physical objects should not be repersented with voids encompassed -> going down
    public Base childrenObject { get; set; } //maybe holding pointers here would make sense more

  }
}
