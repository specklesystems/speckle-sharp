
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB.Architecture;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    private RevitStair StairToSpeckle(DB.Stairs revitStair)
    {
      var baseLevelParam = revitStair.get_Parameter(BuiltInParameter.STAIRS_BASE_LEVEL_PARAM);
      var topLevelParam = revitStair.get_Parameter(BuiltInParameter.STAIRS_TOP_LEVEL_PARAM);


      var speckleStair = new RevitStair();
      speckleStair.type = Doc.GetElement(revitStair.GetTypeId()).Name;
      speckleStair.type = Doc.GetElement(revitStair.GetTypeId()).Name;


      speckleStair.level = ConvertAndCacheLevel(baseLevelParam);
      speckleStair.topLevel = ConvertAndCacheLevel(topLevelParam);

      GetRevitParameters(speckleStair, revitStair, new List<string> { "STAIRS_BASE_LEVEL_PARAM", "STAIRS_TOP_LEVEL_PARAM" });

      var mesh = new Geometry.Mesh();
      (mesh.faces, mesh.vertices) = GetFaceVertexArrayFromElement(revitStair, new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });

      speckleStair["@displayMesh"] = mesh;

      return speckleStair;
    }
  }
}
