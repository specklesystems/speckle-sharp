using System.Collections.Generic;
using Autodesk.Revit.DB;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  public IList<GeometryObject> TeklaMeshToNative(Mesh displayMesh)
  {
    return MeshToNative(displayMesh);
  }
}
