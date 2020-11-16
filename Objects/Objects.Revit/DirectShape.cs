using System;
using System.Collections.Generic;
using System.Text;
using static Objects.Revit.RevitUtils;
using Objects.BuiltElements;
using Objects.Geometry;
using Speckle.Core.Kits;

namespace Objects.Revit
{
  public class DirectShape : RevitElement, IRevit
  {
    // pruned from: https://docs.google.com/spreadsheets/d/1uNa77XYLjeN-1c63gsX6C5D5Pvn_3ZB4B0QMgPeloTw/edit#gid=1549586957
    public RevitCategory category { get; set; }
    public Mesh baseGeometry { get; set; }
  }





}
