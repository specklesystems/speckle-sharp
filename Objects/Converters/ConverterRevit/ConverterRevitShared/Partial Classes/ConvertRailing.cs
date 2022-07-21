using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public List<ApplicationObject> RailingToNative(BuiltElements.Revit.RevitRailing speckleRailing)
    {
      var appObj = new ApplicationObject(speckleRailing.id, speckleRailing.speckle_type) { applicationId = speckleRailing.applicationId };
      
      if (speckleRailing.path == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Only line based Railings are currently supported.");
        return new List<ApplicationObject> { appObj };
      }

      var revitRailing = GetExistingElementByApplicationId(speckleRailing.applicationId) as Railing;
      if (revitRailing != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore)
      {
        appObj.Update(status: ApplicationObject.State.Skipped, createdId: revitRailing.UniqueId, existingObject: revitRailing);
        return new List<ApplicationObject> { appObj };
      }

      var railingType = GetElementType<RailingType>(speckleRailing);
      Level level = ConvertLevelToRevit(speckleRailing.level, out ApplicationObject.State levelState);
      if (level == null) //we currently don't support railings hosted on stairs, and these have null level
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "level was null");
        return new List<ApplicationObject> { appObj };
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
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "could not create revit railing");
        return new List<ApplicationObject> { appObj };
      }

      if (revitRailing.GetTypeId() != railingType.Id)
        revitRailing.ChangeTypeId(railingType.Id);

      if (isUpdate)
      {
        revitRailing.SetPath(baseCurve);
        TrySetParam(revitRailing, BuiltInParameter.WALL_BASE_CONSTRAINT, level);
      }

      if (speckleRailing.flipped != revitRailing.Flipped)
        revitRailing.Flip();

      SetInstanceParameters(revitRailing, speckleRailing);

      var status = isUpdate ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
      appObj.Update(status: status, createdId: revitRailing.UniqueId, existingObject: revitRailing);
      Doc.Regenerate();
      return new List<ApplicationObject> { appObj };
    }

    //TODO: host railings, where possible
    private RevitRailing RailingToSpeckle(Railing revitRailing)
    {
      var railingType = revitRailing.Document.GetElement(revitRailing.GetTypeId()) as RailingType;
      var speckleRailing = new RevitRailing();
      //speckleRailing.family = railingType.FamilyName;
      speckleRailing.type = railingType.Name;
      speckleRailing.level = ConvertAndCacheLevel(revitRailing, BuiltInParameter.STAIRS_RAILING_BASE_LEVEL_PARAM);
      speckleRailing.path = CurveListToSpeckle(revitRailing.GetPath());

      GetAllRevitParamsAndIds(speckleRailing, revitRailing, new List<string> { "STAIRS_RAILING_BASE_LEVEL_PARAM" });

      speckleRailing.displayValue = GetElementDisplayMesh(revitRailing, new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });
      Report.Log($"Converted Railing {revitRailing.Id}");

      return speckleRailing;
    }

  }
}
