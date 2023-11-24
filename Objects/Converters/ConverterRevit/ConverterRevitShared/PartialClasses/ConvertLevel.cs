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
    public DB.Level GetExistingLevelByName(IEnumerable<DB.Level> docLevels, string name)
    {
      return docLevels.FirstOrDefault(x => x.Name == name);
    }

    public DB.Level GetExistingLevelByElevation(IEnumerable<DB.Level> docLevels, double elevation)
    {
      return docLevels.FirstOrDefault(l => Math.Abs(l.Elevation - (double)elevation) < TOLERANCE);
    }

    public DB.Level GetExistingLevelByClosestElevation(IEnumerable<DB.Level> docLevels, double elevation, out double elevationOffset)
    {
      elevationOffset = 0.0;
      DB.Level level = docLevels.LastOrDefault(l => (l.Elevation < elevation + TOLERANCE)) ?? docLevels.FirstOrDefault();

      if (level != null)
        elevationOffset = level.Elevation - elevation;

      return level;
    }

    public DB.Level ConvertLevelToRevit(XYZ point, out ApplicationObject.State state, out double elevationOffset)
    {
      var elevation = ElevationFromPoint(point);
      return ConvertLevelToRevit(ObjectsLevelFromElevation(elevation), false, out state, out elevationOffset);
    }

    public DB.Level ConvertLevelToRevit(Curve curve, out ApplicationObject.State state, out double elevationOffset)
    {
      var elevation = ElevationFromCurve(curve);
      return ConvertLevelToRevit(ObjectsLevelFromElevation(elevation), false, out state, out elevationOffset);
    }

    public DB.Level ConvertLevelToRevit(BuiltElements.Level speckleLevel, out ApplicationObject.State state)
    {
      double elevationOffset = 0.0;
      return ConvertLevelToRevit(speckleLevel, true, out state, out elevationOffset);
    }

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
    public DB.Level ConvertLevelToRevit(BuiltElements.Level speckleLevel, bool exactElevation, out ApplicationObject.State state, out double elevationOffset)
    {
      state = ApplicationObject.State.Unknown;
      elevationOffset = 0.0;
      if (speckleLevel == null) return null;

      var docLevels = new FilteredElementCollector(Doc).OfClass(typeof(DB.Level)).ToElements().Cast<DB.Level>();

      bool elevationMatch = true;
      //level by name component
      if (speckleLevel is RevitLevel speckleRevitLevel && speckleRevitLevel.referenceOnly)
      {
        //see: https://speckle.community/t/revit-connector-levels-and-spaces/2824/5
        elevationMatch = false;
        if (GetExistingLevelByName(docLevels, speckleLevel.name) is DB.Level existingLevelWithSameName)
            return existingLevelWithSameName;
      }

      DB.Level revitLevel = null;
      var speckleLevelElevation = ScaleToNative((double)speckleLevel.elevation, speckleLevel.units);

      //the level has been received before (via schema builder probably)
      //match by appid => update level
      if (GetExistingElementByApplicationId(speckleLevel.applicationId) is DB.Level existingLevelAlreadyReceived)
      {
        revitLevel = existingLevelAlreadyReceived;

        revitLevel.Name = speckleLevel.name;
        if (Math.Abs(revitLevel.Elevation - (double)speckleLevelElevation) >= TOLERANCE)
          revitLevel.Elevation = speckleLevelElevation;

        state = ApplicationObject.State.Updated;
      }
      //match by elevation
      else if (!exactElevation && elevationMatch && (GetExistingLevelByClosestElevation(docLevels, speckleLevelElevation, out elevationOffset) is DB.Level existingLevelWithClosestElevation))
      {
        revitLevel = existingLevelWithClosestElevation;
        state = ApplicationObject.State.Skipped; // state should be eliminated
      }
      //match by elevation
      else if (elevationMatch && (GetExistingLevelByElevation(docLevels, speckleLevelElevation) is DB.Level existingLevelWithSameElevation))
      {
        revitLevel = existingLevelWithSameElevation;
        revitLevel.Name = speckleLevel.name;
        state = ApplicationObject.State.Updated;
      }

      else
      {
        // If we don't have an existing level, create it.
        revitLevel = Level.Create(Doc, (double)speckleLevelElevation);
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

    private double ElevationFromPoint(XYZ point)
    {
      var p = PointToSpeckle(point, Doc);
      return p.z;
    }

    private RevitLevel LevelFromElevation(double z)
    {
      return new RevitLevel() { elevation = z, name = "Generated Level " + z, units = ModelUnits };
    }

    private Objects.BuiltElements.Level ObjectsLevelFromElevation(double z)
    {
      return new Objects.BuiltElements.Level() { elevation = z, name = "Generated Level " + z, units = ModelUnits };
    }

    private RevitLevel LevelFromPoint(XYZ point)
    {
      return LevelFromElevation(ElevationFromPoint(point));
    }

    private double ElevationFromCurve(Curve curve)
    {
      var start = curve.GetEndPoint(0);
      var end = curve.GetEndPoint(1);
      var point = start.Z < end.Z ? start : end; // pick the lowest
      return ElevationFromPoint(point);
    }

    private RevitLevel LevelFromCurve(Curve curve)
    {
      return LevelFromElevation(ElevationFromCurve(curve));
    }

    private Level GetFirstDocLevel()
    {
      var docLevels = new FilteredElementCollector(Doc).OfClass(typeof(Level)).ToElements().Cast<Level>();
      return docLevels.First();
    }
  }
}
