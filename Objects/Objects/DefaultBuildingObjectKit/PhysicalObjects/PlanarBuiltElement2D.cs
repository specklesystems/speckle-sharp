using System;
using System.Collections.Generic;
using System.Text;
using Objects.DefaultBuildingObjectKit.Calculations;
using Objects.DefaultBuildingObjectKit.Visualization;
using Speckle.Core.Models;
using Objects.Geometry;
using Objects.Other;

namespace Objects.DefaultBuildingObjectKit.PhysicalObjects
{
  public class PlanarBuiltElement2D :Base  , IDisplayValue<List<Mesh>> , ISourceAppParams
  {
    public BuiltElement2DProperty BuiltElement2DProperty{ get; set; }
    public Material material { get; set; }

    public ICurve outline { get; set; }//Users should use only either one of the two methods to generate this element in GH. Conversion routines should check for both

    public List<Mesh> displayValue { get; set; } // for visualization
    public double area { get; set; }// the actual area of the 2d element 
    public string name { get; set; }// for tracking 
    public List<Base> Voids { get; set; }//Physical objects can have voids in them, non physical objects should not be repersented with voids encompassed
    public Base childrenObject { get; set; } //maybe holding pointers here would make sense more
    public string sourceAppName { get; set; }
    public List<Base> sourceAppParams { get; set; }
  }

}
