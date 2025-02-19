using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel.Types;
using Objects.Geometry;
using Objects.Primitive;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Beam = Objects.BuiltElements.Beam;
using Ceiling = Objects.BuiltElements.Ceiling;
using Column = Objects.BuiltElements.Column;
using Element1D = Objects.Structural.Geometry.Element1D;
using Element2D = Objects.Structural.Geometry.Element2D;
using Element3D = Objects.Structural.Geometry.Element3D;
using Floor = Objects.BuiltElements.Floor;
using Mesh = Objects.Geometry.Mesh;
using Node = Objects.Structural.Geometry.Node;
using Opening = Objects.BuiltElements.Opening;
using Point = Objects.Geometry.Point;
using RH = Rhino.Geometry;
using Roof = Objects.BuiltElements.Roof;
using RV = Objects.BuiltElements.Revit;
using View3D = Objects.BuiltElements.View3D;
using Wall = Objects.BuiltElements.Wall;

namespace Objects.Converter.RhinoGh
{
  public partial class ConverterRhinoGh
  {
    public Node PointToSpeckleNode(RH.Point point)
    {
      return new Node((Point)ConvertToSpeckle(point)) { units = ModelUnits };
    }

    public Element1D CurveToSpeckleElement1D(RH.Curve curve)
    {
      return new Element1D((ICurve)ConvertToSpeckle(curve)) { units = ModelUnits };
    }

    public Element2D MeshToSpeckleElement2D(RH.Mesh mesh)
    {
      return new Element2D((Mesh)ConvertToSpeckle(mesh)) { units = ModelUnits };
    }

    public Element3D MeshToSpeckleElement3D(RH.Mesh mesh)
    {
      return new Element3D((Mesh)ConvertToSpeckle(mesh)) { units = ModelUnits };
    }
  }
}
