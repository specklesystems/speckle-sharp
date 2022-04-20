using System;
using System.Collections.Generic;
using System.Text;
using Objects.DefaultBuildingObjectKit.GenericElements;
using Objects.DefaultBuildingObjectKit.Calculations;
using Objects.DefaultBuildingObjectKit.Visualization;
using Speckle.Core.Models;

namespace Objects.DefaultBuildingObjectKit.PhysicalObjects
{
  public class PlanarBuiltElement2D :Element2D
  {
    public BuiltElement2DProperty BuiltElement2DProperty{ get; set; }
    public Material material { get; set; }

    public RenderingMaterial renderingMaterial { get; set; }// optional ?
    public List<Base> Voids { get; set; }//Physical objects can have voids in them, non physical objects should not be repersented with voids encompassed
    public Base parentObject { get; set; } 

    public Base childrenObject { get; set; } //maybe holding pointers here would make sense more

  }

}
