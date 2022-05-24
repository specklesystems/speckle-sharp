using System;
using System.Collections.Generic;
using Objects.BuildingObject.enums;
using Speckle.Core.Models;
using Objects.Geometry;
using Objects.Visualization;

namespace Objects.Definitions
{
  public abstract class PointBasedElement : Base, IDisplayValue<List<Mesh>>
  {
    public Point basePoint { get; set; }

    public PointProperty property { get; set; }
    public Material material { get; set; }
    public Base childrenObject { get; set; } //maybe holding pointers here would make sense more
    public List<Mesh> displayValue { get; set; }
  }

  public class PointProperty : Base
  {
    public string name { get; set; }

    public PointPropertyType type { get; set; }
  }
}