using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Speckle.Newtonsoft.Json;

namespace Objects.DefaultBuildingObjectKit.GenericElements
{
  public class Element2D : Base, IDisplayMesh, IHasArea
  {
    public ICurve outline { get; set; }//Users should use only either one of the two methods to generate this element in GH. Conversion routines should check for both
    public List<Point> Topology { get; set; } // List of Points (basically helping support polygons defns) ~ to discuss
    public Mesh displayMesh { get; set; } // for visualization
    public double area { get; set; }// the actual area of the 2d element 
    public string name { get; set; }// for tracking 


  }

}
