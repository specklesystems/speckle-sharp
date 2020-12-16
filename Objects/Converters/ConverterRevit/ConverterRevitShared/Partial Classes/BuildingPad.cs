
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
    private BuildingPad BuildingPadToSpeckle(DB.BuildingPad revitPad)
    {
      var baseLevelParam = revitPad.get_Parameter(BuiltInParameter.LEVEL_PARAM);
      var profiles = GetProfiles(revitPad);

      var specklePad = new BuildingPad();
      specklePad.type = Doc.GetElement(revitPad.GetTypeId()).Name;
      specklePad.outline = profiles[0];
      if (profiles.Count > 1)
      {
        specklePad.voids = profiles.Skip(1).ToList();
      }

      specklePad.level = ConvertAndCacheLevel(baseLevelParam);

      GetRevitParameters(specklePad, revitPad, new List<string> { "LEVEL_PARAM" });

      var mesh = new Geometry.Mesh();
      (mesh.faces, mesh.vertices) = GetFaceVertexArrayFromElement(revitPad, new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });

      specklePad["@displayMesh"] = mesh;

      return specklePad;
    }
  }
}
