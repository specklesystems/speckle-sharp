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
  public class Element3D : Base, IDisplayMesh, IHasVolume
  {
    //should we have something similar to BREP definition here but not a Brep Defn ? 
    public Mesh displayMesh { get; set; } // for visualization
    public double surfaceArea { get; set; }// the actual surface area of the 3d element 
    public string name { get; set; }// for tracking 
    public double volume { get; set ; } //need to have volume on
  }
}
