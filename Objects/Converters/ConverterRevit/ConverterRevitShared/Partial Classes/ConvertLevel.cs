using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {

    /// <summary>
    /// Tries to find a level by ELEVATION only, otherwise it creates it.
    /// Unless it was created before and we still have its app id in memory
    /// Reason for not matching levels by name instead: 
    /// source file has L0 @0m and L1 @4m
    /// dest file has L1 @0m and L2 @4m
    /// attempting to move or rename levels would just be a mess, hence, we don't do that!
    /// </summary>
    /// <param name="speckleLevel"></param>
    /// <returns></returns>
    public DB.Level ConvertLevelToRevit(BuiltElements.Level speckleLevel, out ApplicationObject.State state)
    {
      state = ApplicationObject.State.Unknown;

      var docLevels = new FilteredElementCollector(Doc).OfClass(typeof(DB.Level)).ToElements().Cast<DB.Level>();
      bool elevationMatch = true;
      //level by name component
      if (speckleLevel is RevitLevel speckleRevitLevel && speckleRevitLevel.referenceOnly)
      {
        //see: https://speckle.community/t/revit-connector-levels-and-spaces/2824/5
        elevationMatch = false;
        var l = docLevels.FirstOrDefault(x => x.Name == speckleLevel.name);
        if (l != null)
          return l;
      }

      if (speckleLevel == null) return null;
      var speckleLevelElevation = ScaleToNative((double)speckleLevel.elevation, speckleLevel.units);

      var hasLevelWithSameName = docLevels.Any(x => x.Name == speckleLevel.name);
      Level existingLevelWithSameElevation = null;
      if (elevationMatch)
        existingLevelWithSameElevation = docLevels.FirstOrDefault(l => Math.Abs(l.Elevation - (double)speckleLevelElevation) < TOLERANCE);

      //a level that had been previously received
      var revitLevel = GetExistingElementByApplicationId(speckleLevel.applicationId) as DB.Level;

      //the level has been received before (via schema builder probably)
      //match by appid => update level
      if (revitLevel != null)
      {
        //update name
        if (!hasLevelWithSameName)
          revitLevel.Name = speckleLevel.name;

        if (Math.Abs(revitLevel.Elevation - (double)speckleLevelElevation) >= TOLERANCE)
          revitLevel.Elevation = speckleLevelElevation;

        state = ApplicationObject.State.Updated;
      }
      //match by elevation
      else if (existingLevelWithSameElevation != null)
      {
        revitLevel = existingLevelWithSameElevation;
        if (!hasLevelWithSameName)
        {
          revitLevel.Name = speckleLevel.name;
          state = ApplicationObject.State.Updated;
        }
      }

      else
      {
        // If we don't have an existing level, create it.
        revitLevel = Level.Create(Doc, (double)speckleLevelElevation);
        if (!hasLevelWithSameName)
          revitLevel.Name = speckleLevel.name;
        var rl = speckleLevel as RevitLevel;
        if (rl != null && rl.createView)
          CreateViewPlan(speckleLevel.name, revitLevel.Id);

        state = ApplicationObject.State.Created;
      }

      return revitLevel;
    }

    public ApplicationObject LevelToNative(BuiltElements.Level speckleLevel)
    {
      var revitLevel = ConvertLevelToRevit(speckleLevel, out ApplicationObject.State state);
      var appObj = new ApplicationObject(speckleLevel.id, speckleLevel.speckle_type) { applicationId = speckleLevel.applicationId };
      appObj.Update(status: state, createdId: revitLevel.UniqueId, convertedItem: revitLevel);
      return appObj;
    }

    public RevitLevel LevelToSpeckle(DB.Level revitLevel)
    {
      var speckleLevel = new RevitLevel();

      speckleLevel.elevation = ScaleToSpeckle(revitLevel.Elevation);
      speckleLevel.name = revitLevel.Name;
      speckleLevel.createView = true;

      GetAllRevitParamsAndIds(speckleLevel, revitLevel);

      return speckleLevel;
    }

    public ViewPlan CreateViewPlan(string name, ElementId levelId)
    {
      var vt = new FilteredElementCollector(Doc).OfClass(typeof(ViewFamilyType)).Where(el => ((ViewFamilyType)el).ViewFamily == ViewFamily.FloorPlan).First();

      var view = ViewPlan.Create(Doc, vt.Id, levelId);
      try
      {
        view.Name = name;
      }
      catch { }

      Report.Log($"Created ViewPlan {view.Id}");

      return view;
    }

    private Level GetLevelByName(string name)
    {
      var collector = new FilteredElementCollector(Doc).OfClass(typeof(DB.Level)).ToElements().Cast<DB.Level>();

      //match by name
      var revitLevel = collector.FirstOrDefault(x => x.Name == name);
      if (revitLevel != null)
        return revitLevel;

      //match by id?
      revitLevel = collector.FirstOrDefault(x => x.Id.ToString() == name);
      if (revitLevel != null)
        return revitLevel;

      Report.LogConversionError(new Exception($"Could not find level `{name}`, a default level will be used."));

      return collector.FirstOrDefault();
    }

    private RevitLevel ConvertAndCacheLevel(DB.Element elem, BuiltInParameter bip)
    {
      var param = elem.get_Parameter(bip);

      if (param == null || param.StorageType != StorageType.ElementId)
        return null;

      return ConvertAndCacheLevel(param.AsElementId(), elem.Document);
    }

    private RevitLevel ConvertAndCacheLevel(ElementId id, Document doc)
    {
      var level = doc.GetElement(id) as DB.Level;

      if (level == null) return null;
      if (!Levels.ContainsKey(level.Name))
        Levels[level.Name] = LevelToSpeckle(level);

      return Levels[level.Name] as RevitLevel;
    }

    private RevitLevel LevelFromPoint(XYZ point)
    {
      var p = PointToSpeckle(point, Doc);
      return new RevitLevel() { elevation = p.z, name = "Generated Level " + p.z, units = ModelUnits };
    }

    private RevitLevel LevelFromCurve(Curve curve)
    {
      var start = curve.GetEndPoint(0);
      var end = curve.GetEndPoint(1);
      var point = start.Z < end.Z ? start : end; // pick the lowest
      return LevelFromPoint(point);
    }

    private Level GetFirstDocLevel()
    {
      var docLevels = new FilteredElementCollector(Doc).OfClass(typeof(Level)).ToElements().Cast<Level>();
      return docLevels.First();
    }
  }
}
