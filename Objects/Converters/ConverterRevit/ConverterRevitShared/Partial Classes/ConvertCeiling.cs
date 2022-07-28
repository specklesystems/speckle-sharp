﻿
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
    private RevitCeiling CeilingToSpeckle(DB.Ceiling revitCeiling, out List<string> notes)
    {
      notes = new List<string>();
      var profiles = GetProfiles(revitCeiling);

      var speckleCeiling = new RevitCeiling();
      speckleCeiling.type = revitCeiling.Document.GetElement(revitCeiling.GetTypeId()).Name;
      speckleCeiling.outline = profiles[0];
      if (profiles.Count > 1)
        speckleCeiling.voids = profiles.Skip(1).ToList();

      speckleCeiling.offset = GetParamValue<double>(revitCeiling, BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM);
      speckleCeiling.level = ConvertAndCacheLevel(revitCeiling, BuiltInParameter.LEVEL_PARAM);

      GetAllRevitParamsAndIds(speckleCeiling, revitCeiling, new List<string> { "LEVEL_PARAM", "CEILING_HEIGHTABOVELEVEL_PARAM" });

      GetHostedElements(speckleCeiling, revitCeiling, out List<string> hostedNotes);
      if (hostedNotes.Any()) notes.AddRange(hostedNotes);

      speckleCeiling.displayValue = GetElementDisplayMesh(revitCeiling, new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });

      return speckleCeiling;
    }

#if REVIT2022
    public ApplicationObject CeilingToNative(Ceiling speckleCeiling)
    {
      var appObj = new ApplicationObject(speckleCeiling.id, speckleCeiling.speckle_type) { applicationId = speckleCeiling.applicationId };
      if (speckleCeiling.outline == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Missing an outline curve.");
        return appObj;
      }

      var outline = CurveToNative(speckleCeiling.outline);
      var profile = new CurveLoop();
      foreach (DB.Curve segment in outline)
        profile.Append(segment);

      DB.Level level = null;
      double slope = 0;
      DB.Line slopeDirection = null;
      var levelState = ApplicationObject.State.Unknown;
      if (speckleCeiling is RevitCeiling speckleRevitCeiling)
      {
        level = ConvertLevelToRevit(speckleRevitCeiling.level, out levelState);
        slope = speckleRevitCeiling.slope;
        slopeDirection = (speckleRevitCeiling.slopeDirection != null) ? LineToNative(speckleRevitCeiling.slopeDirection) : null;
      }
      else
      {
        level = ConvertLevelToRevit(LevelFromCurve(outline.get_Item(0)), out levelState);
      }

      var ceilingType = GetElementType<CeilingType>(speckleCeiling);

      var docObj = GetExistingElementByApplicationId(speckleCeiling.applicationId);
      if (docObj != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore)
      {
        appObj.Update(status: ApplicationObject.State.Skipped, createdId: docObj.UniqueId, convertedItem: docObj);
        return appObj;
      }
   
      if (docObj != null)
        Doc.Delete(docObj.Id);

      DB.Ceiling revitCeiling;
      
      if (slope != 0 && slopeDirection != null)
        revitCeiling = DB.Ceiling.Create(Doc, new List<CurveLoop> { profile }, ceilingType.Id, level.Id, slopeDirection, slope);
      else
        revitCeiling = DB.Ceiling.Create(Doc, new List<CurveLoop> { profile }, ceilingType.Id, level.Id);

      Doc.Regenerate();

      try
      {
        CreateVoids(revitCeiling, speckleCeiling);
      }
      catch (Exception ex)
      {
        appObj.Update(logItem: $"Could not create openings: {ex.Message}");
      }

      SetInstanceParameters(revitCeiling, speckleCeiling);

      appObj.Update(status: ApplicationObject.State.Created, createdId: revitCeiling.UniqueId, convertedItem: revitCeiling);
      appObj = SetHostedElements(speckleCeiling, revitCeiling, appObj);
      return appObj;
    }
#endif
  }
}
