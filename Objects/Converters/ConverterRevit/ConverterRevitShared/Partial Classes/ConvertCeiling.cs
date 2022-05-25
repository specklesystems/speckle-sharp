
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Ceiling = Objects.BuiltElements.Ceiling;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    private RevitCeiling CeilingToSpeckle(DB.Ceiling revitCeiling)
    {
      var profiles = GetProfiles(revitCeiling);

      var speckleCeiling = new RevitCeiling();
      speckleCeiling.type = revitCeiling.Document.GetElement(revitCeiling.GetTypeId()).Name;
      speckleCeiling.outline = profiles[0];
      if (profiles.Count > 1)
      {
        speckleCeiling.voids = profiles.Skip(1).ToList();
      }
      speckleCeiling.offset = GetParamValue<double>(revitCeiling, BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM);
      speckleCeiling.level = ConvertAndCacheLevel(revitCeiling, BuiltInParameter.LEVEL_PARAM);


      GetAllRevitParamsAndIds(speckleCeiling, revitCeiling, new List<string> { "LEVEL_PARAM", "CEILING_HEIGHTABOVELEVEL_PARAM" });

      GetHostedElements(speckleCeiling, revitCeiling);

      speckleCeiling.displayValue = GetElementDisplayMesh(revitCeiling, new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });
      Report.Log($"Converted BuildingPad {revitCeiling.Id}");

      return speckleCeiling;
    }

#if REVIT2022
    public List<ApplicationPlaceholderObject> CeilingToNative(Ceiling speckleCeiling)
    {
      if (speckleCeiling.outline == null)
      {
        throw new Speckle.Core.Logging.SpeckleException("Ceiling is missing an outline curve.");
      }

      var outline = CurveToNative(speckleCeiling.outline);
      var profile = new CurveLoop();
      foreach (DB.Curve segment in outline)
      {
        profile.Append(segment);
      }

      DB.Level level = null;
      double slope = 0;
      DB.Line slopeDirection = null;
      if (speckleCeiling is RevitCeiling speckleRevitCeiling)
      {
        level = ConvertLevelToRevit(speckleRevitCeiling.level);
        slope = speckleRevitCeiling.slope;
        slopeDirection = (speckleRevitCeiling.slopeDirection != null) ? LineToNative(speckleRevitCeiling.slopeDirection) : null;
      }
      else
      {
        level = ConvertLevelToRevit(LevelFromCurve(outline.get_Item(0)));
      }

      var ceilingType = GetElementType<CeilingType>(speckleCeiling);

      var docObj = GetExistingElementByApplicationId(speckleCeiling.applicationId);
      if (docObj != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore)
        return new List<ApplicationPlaceholderObject> { new ApplicationPlaceholderObject { applicationId = speckleCeiling.applicationId, ApplicationGeneratedId = docObj.UniqueId, NativeObject = docObj } };
      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }

      DB.Ceiling revitCeiling;
      
      if (slope != 0 && slopeDirection != null)
      {
        revitCeiling = DB.Ceiling.Create(Doc, new List<CurveLoop> { profile }, ceilingType.Id, level.Id, slopeDirection, slope);
      }
      else
      {
        revitCeiling = DB.Ceiling.Create(Doc, new List<CurveLoop> { profile }, ceilingType.Id, level.Id);
      }

      Doc.Regenerate();

      try
      {
        CreateVoids(revitCeiling, speckleCeiling);
      }
      catch (Exception ex)
      {
        Report.LogConversionError(new Exception($"Could not create openings in ceiling {speckleCeiling.applicationId}", ex));
      }

      SetInstanceParameters(revitCeiling, speckleCeiling);

      var placeholders = new List<ApplicationPlaceholderObject>() { new ApplicationPlaceholderObject { applicationId = speckleCeiling.applicationId, ApplicationGeneratedId = revitCeiling.UniqueId, NativeObject = revitCeiling } };

      var hostedElements = SetHostedElements(speckleCeiling, revitCeiling);
      placeholders.AddRange(hostedElements);
      Report.Log($"Created Ceiling {revitCeiling.Id}");
      return placeholders;
    }
#endif
  }
}
