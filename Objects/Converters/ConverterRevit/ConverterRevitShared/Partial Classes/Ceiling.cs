
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {

    //NOTE: there is not API method to create new ceiling
    //a possible workaround is to duplicate an existing one and edit its profile

    private RevitCeiling CeilingToSpeckle(DB.Ceiling revitCeiling)
    {
      var baseLevelParam = revitCeiling.get_Parameter(BuiltInParameter.LEVEL_PARAM);
      var offsetParam = revitCeiling.get_Parameter(BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM);

      var profiles = GetProfiles(revitCeiling);

      var speckleCeiling = new RevitCeiling();
      speckleCeiling.type = Doc.GetElement(revitCeiling.GetTypeId()).Name;
      speckleCeiling.outline = profiles[0];
      if (profiles.Count > 1)
      {
        speckleCeiling.voids = profiles.Skip(1).ToList();
      }
      speckleCeiling.offset = (double)ParameterToSpeckle(offsetParam).value;
      speckleCeiling.level = ConvertAndCacheLevel(baseLevelParam);

      GetRevitParameters(speckleCeiling, revitCeiling, new List<string> { "LEVEL_PARAM", "CEILING_HEIGHTABOVELEVEL_PARAM" });

      var mesh = new Geometry.Mesh();
      (mesh.faces, mesh.vertices) = GetFaceVertexArrayFromElement(revitCeiling, new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });

      speckleCeiling["@displayMesh"] = mesh;

      return speckleCeiling;
    }

  }
}
