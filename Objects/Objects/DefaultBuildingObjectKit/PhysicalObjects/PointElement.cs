using System;
using System.Collections.Generic;
using System.Text;
using Objects.DefaultBuildingObjectKit.Calculations;
using Objects.DefaultBuildingObjectKit.Visualization;
using Speckle.Core.Models;
using Objects.Geometry;

namespace Objects.DefaultBuildingObjectKit.PhysicalObjects
{
  public abstract class PointElement : Base , IDisplayValue<List<Mesh>> 
  {
    public Point basePoint { get; set; }
    //public string name { get; set; }
    public PointProperty property { get; set; }
    public Material material { get; set; }
    public Base childrenObject { get; set; } //maybe holding pointers here would make sense more
    public List<Mesh> displayValue { get; set; }
  }


}
