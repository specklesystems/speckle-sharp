using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {

    /// <summary>
    /// Tries to find a level by ELEVATION only, otherwise it creates it.
    /// Unless it was created with schema builder and has `referenceOnly=true`, in which case it gets it by name only
    /// Reason for this approach, take the example below: 
    /// source file has L0 @0m and L1 @4m
    /// dest file has L1 @0m and L2 @4m
    /// attempting to move or rename levels would just be a mess, hence, we don't do that!
    /// </summary>
    /// <param name="speckleLevel"></param>
    /// <returns></returns>
    public Level LevelToNative(BuiltElements.Level speckleLevel)
    {
      if (speckleLevel == null) return null;
      var docLevels = new FilteredElementCollector(Doc).OfClass(typeof(DB.Level)).ToElements().Cast<DB.Level>();

      // it's a level created with schema builder for reference only
      // we only try to match it by name
      var rl = speckleLevel as RevitLevel;
      if (rl != null && rl.referenceOnly)
      {
        var existingLevel = docLevels.FirstOrDefault(docLevel => docLevel.Name == speckleLevel.name);
        if (existingLevel != null)
          return existingLevel;
        else
        {
          ConversionErrors.Add(new Exception($"Could not find level '{speckleLevel.name}' in this document."));
          return null;
        }
      }

      var speckleLevelElevation = ScaleToNative((double)speckleLevel.elevation, speckleLevel.units);
      var existingLevelByElevation = docLevels.FirstOrDefault(l => Math.Abs(l.Elevation - (double)speckleLevelElevation) < 0.0164042);
      if (existingLevelByElevation != null)
        return existingLevelByElevation;

      // If we don't have an existing level, create it.
      var level = Level.Create(Doc, (double)speckleLevelElevation);
      if (!docLevels.Any(x => x.Name == speckleLevel.name))
        level.Name = speckleLevel.name;
      if (rl != null && rl.createView)
      {
        CreateViewPlan(speckleLevel.name, level.Id);
      }
      return level;

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

    private void CreateViewPlan(string name, ElementId levelId)
    {
      var vt = new FilteredElementCollector(Doc).OfClass(typeof(ViewFamilyType)).Where(el => ((ViewFamilyType)el).ViewFamily == ViewFamily.FloorPlan).First();

      var view = ViewPlan.Create(Doc, vt.Id, levelId);
      try
      {
        view.Name = name;
      }
      catch { }
    }

    private Level GetLevelByName(string name)
    {
      var collector = new FilteredElementCollector(Doc).OfClass(typeof(DB.Level)).ToElements().Cast<DB.Level>();

      //match by name
      var revitLevel = collector.FirstOrDefault(x => x.Name == name);
      if (revitLevel != null)
      {
        return revitLevel;
      }

      //match by id?
      revitLevel = collector.FirstOrDefault(x => x.Id.ToString() == name);
      if (revitLevel != null)
      {
        return revitLevel;
      }

      ConversionErrors.Add(new Exception($"Could not find level `{name}`, a default level will be used."));

      return collector.FirstOrDefault();
    }

    private RevitLevel ConvertAndCacheLevel(DB.Element elem, BuiltInParameter bip)
    {
      var param = elem.get_Parameter(bip);

      if (param == null || param.StorageType != StorageType.ElementId)
      {
        return null;
      }

      return ConvertAndCacheLevel(param.AsElementId());
    }

    private RevitLevel ConvertAndCacheLevel(ElementId id)
    {
      var level = Doc.GetElement(id) as DB.Level;

      if (level == null) return null;
      if (!Levels.ContainsKey(level.Name))
      {
        Levels[level.Name] = LevelToSpeckle(level);
      }

      return Levels[level.Name] as RevitLevel;
    }

    private RevitLevel LevelFromPoint(XYZ point)
    {
      var p = PointToSpeckle(point);
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