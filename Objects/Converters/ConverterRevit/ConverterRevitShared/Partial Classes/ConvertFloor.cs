using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public List<ApplicationPlaceholderObject> FloorToNative(BuiltElements.Floor speckleFloor)
    {
      if (speckleFloor.outline == null)
      {
        throw new Speckle.Core.Logging.SpeckleException("Floor is missing an outline.");
      }

      bool structural = false;
      var outline = CurveToNative(speckleFloor.outline, true);
      UnboundCurveIfSingle(outline);
      DB.Level level;
      double slope = 0;
      DB.Line slopeDirection = null;
      if (speckleFloor is RevitFloor speckleRevitFloor)
      {
        level = ConvertLevelToRevit(speckleRevitFloor.level);
        structural = speckleRevitFloor.structural;
        slope = speckleRevitFloor.slope;
        slopeDirection = (speckleRevitFloor.slopeDirection != null) ? LineToNative(speckleRevitFloor.slopeDirection) : null;
      }
      else
      {
        level = ConvertLevelToRevit(LevelFromCurve(outline.get_Item(0)));
      }

      var floorType = GetElementType<FloorType>(speckleFloor);

      // NOTE: I have not found a way to edit a slab outline properly, so whenever we bake, we renew the element. The closest thing would be:
      // https://adndevbConversionLog.Add.typepad.com/aec/2013/10/change-the-boundary-of-floorsslabs.html
      // This would only work if the floors have the same number (and type!!!) of outline curves. 
      var docObj = GetExistingElementByApplicationId(speckleFloor.applicationId);
      if (docObj != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore)
        return new List<ApplicationPlaceholderObject>
      {
        new ApplicationPlaceholderObject
          {applicationId = speckleFloor.applicationId, ApplicationGeneratedId = docObj.UniqueId, NativeObject = docObj}
      };

      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }

      DB.Floor revitFloor = null;
#if (REVIT2019 || REVIT2020 || REVIT2021)
      if (floorType == null)
      {
        if (slope != 0 && slopeDirection != null)
          revitFloor = Doc.Create.NewSlab(outline, level, slopeDirection, slope, structural);
        if (revitFloor == null)
          revitFloor = Doc.Create.NewFloor(outline, structural);
      }
      else
      {
        if (slope != 0 && slopeDirection != null)
          revitFloor = Doc.Create.NewSlab(outline, level, slopeDirection, slope, structural);
        if (revitFloor == null)
          revitFloor = Doc.Create.NewFloor(outline, floorType, level, structural);
      }

#else
     if (floorType == null)
      {
        throw new SpeckleException("Floor needs a floor type");
      }
      else
      {
        //from revit 2022 we can create openings in the floors!
        var profile = new List<CurveLoop> { CurveArrayToCurveLoop(outline) };
        if(speckleFloor["voids"] != null && (speckleFloor["voids"] is List<ICurve> voids))
        {
          foreach (var v in voids)
          {
            var opening = CurveArrayToCurveLoop(CurveToNative(v, true));
            profile.Add(opening);
          }   
        }


        if (slope != 0 && slopeDirection != null)
          revitFloor = Floor.Create(Doc, profile, floorType.Id, level.Id, structural, slopeDirection, slope);
        if (revitFloor == null)
          revitFloor = Floor.Create(Doc, profile, floorType.Id, level.Id);
      }
#endif


      Doc.Regenerate();

      #if (REVIT2019 || REVIT2020 || REVIT2021)
      try
      {
        CreateVoids(revitFloor, speckleFloor);
      }
      catch (Exception ex)
      {
        Report.LogConversionError(new Exception($"Could not create openings in floor {speckleFloor.applicationId}", ex));
      }
      #endif

      SetInstanceParameters(revitFloor, speckleFloor);

      var placeholders = new List<ApplicationPlaceholderObject>() { new ApplicationPlaceholderObject { applicationId = speckleFloor.applicationId, ApplicationGeneratedId = revitFloor.UniqueId, NativeObject = revitFloor } };

      var hostedElements = SetHostedElements(speckleFloor, revitFloor);
      placeholders.AddRange(hostedElements);
      Report.Log($"Created Floor {revitFloor.Id}");
      return placeholders;
    }

    private RevitFloor FloorToSpeckle(DB.Floor revitFloor)
    {
      var profiles = GetProfiles(revitFloor);

      var speckleFloor = new RevitFloor();
      speckleFloor.type = revitFloor.Document.GetElement(revitFloor.GetTypeId()).Name;
      speckleFloor.outline = profiles[0];
      if (profiles.Count > 1)
      {
        speckleFloor.voids = profiles.Skip(1).ToList();
      }

      speckleFloor.level = ConvertAndCacheLevel(revitFloor, BuiltInParameter.LEVEL_PARAM);
      speckleFloor.structural = GetParamValue<bool>(revitFloor, BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL);

      GetAllRevitParamsAndIds(speckleFloor, revitFloor, new List<string> { "LEVEL_PARAM", "FLOOR_PARAM_IS_STRUCTURAL" });

      speckleFloor.displayValue = GetElementDisplayMesh(revitFloor, new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });

      GetHostedElements(speckleFloor, revitFloor);
      Report.Log($"Converted Floor {revitFloor.Id}");
      return speckleFloor;
    }

    // Nesting the various profiles into a polycurve segments. 
    // TODO: **These should be HORIZONTAL on the floor level!** otherwise sloped floors will not be converted back to native properly
    private List<ICurve> GetProfiles(DB.CeilingAndFloor floor)
    {
      var profiles = new List<ICurve>();
      var faces = HostObjectUtils.GetTopFaces(floor);
      Face face = floor.GetGeometryObjectFromReference(faces[0]) as Face;
      var crvLoops = face.GetEdgesAsCurveLoops();
      foreach (var crvloop in crvLoops)
      {
        var poly = new Polycurve(ModelUnits);
        foreach (var curve in crvloop)
        {
          var c = curve;

          if (c == null)
          {
            continue;
          }
          poly.segments.Add(CurveToSpeckle(c));
        }
        profiles.Add(poly);
      }
      return profiles;
    }
  }
}
