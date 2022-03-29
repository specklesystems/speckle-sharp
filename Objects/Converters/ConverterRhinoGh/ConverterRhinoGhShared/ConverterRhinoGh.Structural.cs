using Grasshopper.Kernel.Types;
using Objects.Geometry;
using Objects.Primitive;
using Rhino.Geometry;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry.Collections;
using Speckle.Core.Models;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Linq;
using Alignment = Objects.BuiltElements.Alignment;
using Column = Objects.BuiltElements.Column;
using Beam = Objects.BuiltElements.Beam;
using Wall = Objects.BuiltElements.Wall;
using Floor = Objects.BuiltElements.Floor;
using Ceiling = Objects.BuiltElements.Ceiling;
using Roof = Objects.BuiltElements.Roof;
using Opening = Objects.BuiltElements.Opening;
using Point = Objects.Geometry.Point;
using View3D = Objects.BuiltElements.View3D;
using RH = Rhino.Geometry;
using RV = Objects.BuiltElements.Revit;
using Objects.Structural.Geometry;
namespace Objects.Converter.RhinoGh
{
  public partial class ConverterRhinoGh
  {
  RH.Curve element1DToNative(Element1D element1d){
      return CurveToNative(element1d.baseLine);
  }
  
  }
}
