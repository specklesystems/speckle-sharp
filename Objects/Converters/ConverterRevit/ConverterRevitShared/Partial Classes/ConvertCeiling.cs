
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
      var profiles = GetProfiles(revitCeiling);

      var speckleCeiling = new RevitCeiling();
      speckleCeiling.type = Doc.GetElement(revitCeiling.GetTypeId()).Name;
      speckleCeiling.outline = profiles[0];
      if (profiles.Count > 1)
      {
        speckleCeiling.voids = profiles.Skip(1).ToList();
      }
      speckleCeiling.offset = GetParamValue<double>(revitCeiling, BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM);
      speckleCeiling.level = ConvertAndCacheLevel(revitCeiling, BuiltInParameter.LEVEL_PARAM);

      GetAllRevitParamsAndIds(speckleCeiling, revitCeiling, new List<string> { "LEVEL_PARAM", "CEILING_HEIGHTABOVELEVEL_PARAM" });

      GetHostedElements(speckleCeiling, revitCeiling);
      speckleCeiling.displayMesh = GetElementDisplayMesh(revitCeiling, new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });

      return speckleCeiling;
    }

  }
}
