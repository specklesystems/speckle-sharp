using Autodesk.Revit.DB;
using Objects.Other;
using System;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Revit
{ 
  public partial class ConverterRevit
  {
    public IList<GeometryObject>  TeklaMeshToNative(Mesh displayMesh){
      return MeshToNative(displayMesh);
    }
  }
}
