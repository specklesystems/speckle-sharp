using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject RailingToNative(BuiltElements.Revit.RevitRailing speckleRailing)
    {
      var revitRailing = GetExistingElementByApplicationId(speckleRailing.applicationId) as Railing;
      var appObj = new ApplicationObject(speckleRailing.id, speckleRailing.speckle_type) { applicationId = speckleRailing.applicationId };

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(revitRailing, appObj))
        return appObj;

      if (speckleRailing.path == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Path was null");
        return appObj;
      }

      var railingType = GetElementType<RailingType>(speckleRailing, appObj, out bool isExactMatch);
      if (railingType == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed);
        return appObj;
      }

      Level level = ConvertLevelToRevit(speckleRailing.level, out ApplicationObject.State levelState);
      if (level == null) //we currently don't support railings hosted on stairs, and these have null level
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Level was null");
        return appObj;
      }

      var baseCurve = CurveArrayToCurveLoop(CurveToNative(speckleRailing.path));

      //if it's a new element, we don't need to update certain properties
      bool isUpdate = true;
      if (revitRailing == null)
      {
        isUpdate = false;
        revitRailing = Railing.Create(Doc, baseCurve, railingType.Id, level.Id);
      }
      if (revitRailing == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Creation returned null");
        return appObj;
      }

      if (isExactMatch && revitRailing.GetTypeId() != railingType.Id)
      {
        revitRailing.ChangeTypeId(railingType.Id);
      }

      if (speckleRailing.topRail != null)
      {
        var topRailType = GetElementType<TopRailType>(speckleRailing.topRail, appObj, out bool isTopRailExactMatch);

        if (GetParamValue<int>(railingType, BuiltInParameter.RAILING_SYSTEM_HAS_TOP_RAIL) == 0)
          TrySetParam(railingType, BuiltInParameter.RAILING_SYSTEM_HAS_TOP_RAIL, 1);

        if (topRailType != null && isTopRailExactMatch)
          railingType.TopRailType = topRailType.Id;

      }


      if (isUpdate)
      {
        revitRailing.SetPath(baseCurve);
        TrySetParam(revitRailing, BuiltInParameter.WALL_BASE_CONSTRAINT, level);
      }

      if (speckleRailing.flipped != revitRailing.Flipped)
        revitRailing.Flip();

      SetInstanceParameters(revitRailing, speckleRailing);

      if (speckleRailing.topRail != null)
      {
        // This call to regenerate is to reflect the generation 
        // of the TopRail element associated with the Railing element
        Doc.Regenerate();

        var revitTopRail = Doc.GetElement(revitRailing.TopRail);

        SetInstanceParameters(revitTopRail, speckleRailing.topRail);
      }

      var status = isUpdate ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
      appObj.Update(status: status, createdId: revitRailing.UniqueId, convertedItem: revitRailing);
      Doc.Regenerate();
      return appObj;
    }

    //TODO: host railings, where possible
    private RevitRailing RailingToSpeckle(Railing revitRailing)
    {
      var railingType = revitRailing.Document.GetElement(revitRailing.GetTypeId()) as RailingType;
      var speckleRailing = new RevitRailing();
      //speckleRailing.family = railingType.FamilyName;
      speckleRailing.type = railingType.Name;
      speckleRailing.level = ConvertAndCacheLevel(revitRailing, BuiltInParameter.STAIRS_RAILING_BASE_LEVEL_PARAM);
      speckleRailing.path = CurveListToSpeckle(revitRailing.GetPath(), revitRailing.Document);

      GetAllRevitParamsAndIds(speckleRailing, revitRailing, new List<string> { "STAIRS_RAILING_BASE_LEVEL_PARAM" });

      speckleRailing.displayValue = GetElementDisplayValue(
        revitRailing,
        new Options() { DetailLevel = ViewDetailLevel.Fine }
      );

      if (revitRailing.TopRail != ElementId.InvalidElementId)
      {

        if (ContextObjects.ContainsKey(revitRailing.UniqueId))
        {
          ContextObjects.Remove(revitRailing.UniqueId);
        }

        var revitTopRail = revitRailing.Document.GetElement(revitRailing.TopRail) as TopRail;

        if (ContextObjects.ContainsKey(revitTopRail.UniqueId))
        {
          ContextObjects.Remove(revitTopRail.UniqueId);
        }

        if (CanConvertToSpeckle(revitTopRail))
        {
          speckleRailing.topRail = TopRailToSpeckle(revitTopRail);

          //ensure top rail mesh is visible in viewer
          //currently only the top level displayValue is visualized (or anything under 'elements')
          //if this leads to duplicated meshes in some cases, we might need to remove the display mesh form the TopRail element
          speckleRailing.displayValue.AddRange(speckleRailing.topRail.displayValue);
          ConvertedObjects.Add(speckleRailing.topRail.applicationId);
        }
      }
      return speckleRailing;
    }

  }
}
