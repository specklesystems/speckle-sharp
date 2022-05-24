using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuildingObject.Calculations;
using Speckle.Core.Models;
using Objects.Geometry;
using Objects.Visualization;

namespace Objects.BuildingObject.PhysicalObjects
{
  public abstract class PointBasedElement : Base , IDisplayValue<List<Mesh>> 
  {
    public Point basePoint { get; set; }
    //public string name { get; set; }
    public PointProperty property { get; set; }
    public Material material { get; set; }
    public Base childrenObject { get; set; } //maybe holding pointers here would make sense more
    public List<Mesh> displayValue { get; set; }
  }


}
