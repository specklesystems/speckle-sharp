using Autodesk.Revit.DB;
using Objects.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {

    private Dictionary<string, Level> modifiedLevels = new Dictionary<string, Level>();

    public DB.Level LevelToNative(ILevel speckleLevel)
    {

      var docLevels = new FilteredElementCollector(Doc).OfClass(typeof(DB.Level)).ToElements().Cast<DB.Level>();
      var existingLevelByName = docLevels.FirstOrDefault(docLevel => docLevel.Name == speckleLevel.name);

      Level returnLevel = null;

      // 1) If we have an elevation present:
      if (speckleLevel.elevation != null)
      {
        var speckleLevelElevation = ScaleToNative((double)speckleLevel.elevation, speckleLevel.units);
        var existingLevelByElevation = docLevels.FirstOrDefault(docLevel => Math.Abs(docLevel.Elevation - speckleLevelElevation) < 0.0328084); // NOTE: 1cm tolerance.

        // 1.a) If we have an existing level at that elevation, just return it.
        if (existingLevelByElevation != null)
        {
          returnLevel = existingLevelByElevation;
        }

        // At this stage, we know for sure that there is no level in the doc at the current elevation.

        // 1.b) If we have an existing level by name, and it's the first time we encounter it, let's modify its elevation. 
        if (existingLevelByElevation == null && existingLevelByName != null && !modifiedLevels.ContainsKey(speckleLevel.name))
        {
          existingLevelByName.Elevation = speckleLevelElevation;
          modifiedLevels[existingLevelByName.Name] = existingLevelByName; // let's make sure we keep track of the fact that we modified it.
          returnLevel = existingLevelByName;
        }

        // 1.c) We're now in "whoopsie" mode: we have another level with the same name but different elevation. This is something revit doesn't like!
        if (existingLevelByElevation == null && existingLevelByName != null && modifiedLevels.ContainsKey(speckleLevel.name))
        {
          var newLevel = Level.Create(Doc, speckleLevelElevation); // Leave it revit to set the name.
          returnLevel = newLevel;
          ConversionErrors.Add(new Error { details = $"Specifically, ${existingLevelByName.Name} has been found with an elevation of {speckleLevelElevation} and {modifiedLevels[speckleLevel.name].Elevation}. Speckle has created a new level at the new elevation, with a generated name.", message = $"Found levels with same name but different elevations." });
        }

        // 1.d) We don't have any existing levels of any sort, so let's make it! 
        if (existingLevelByElevation == null && existingLevelByName == null)
        {
          returnLevel = Level.Create(Doc, speckleLevelElevation);
          returnLevel.Name = speckleLevel.name;
          modifiedLevels[speckleLevel.name] = returnLevel;
        }
      }

      // 2) If we have an existing level by its name, and we didn't find a level by elevation (see 1), we should just return the level by name.
      if (existingLevelByName != null && returnLevel == null)
      {
        returnLevel = existingLevelByName;
      }

      // 3) At this stage, if we don't have a level, we need error out. 
      if (returnLevel == null)
      {
        ConversionErrors.Add(new Error { message = "Could not create level.", details = $"Level ${speckleLevel.name} had no elevation and no corresponding document level with that name." });
        return null;
      }

      if(speckleLevel is RevitLevel speckleRevitLevel)
      {
        if(speckleRevitLevel.createView)
        {
          CreateViewPlan(speckleLevel.name, returnLevel.Id);
        }

        SetElementParams(returnLevel, speckleRevitLevel);
      }

      return returnLevel;
    }

    public RevitLevel LevelToSpeckle(DB.Level revitLevel)
    {
      var speckleLevel = new RevitLevel();
      //TODO: check why using Scale?
      speckleLevel.elevation = ScaleToSpeckle(revitLevel.Elevation);
      speckleLevel.name = revitLevel.Name;

      AddCommonRevitProps(speckleLevel, revitLevel);
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

    private DB.Level GetLevelByName(string name)
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

      ConversionErrors.Add(new Speckle.Core.Models.Error($"Could not find level `{name}`", "A default level will be used."));

      return collector.FirstOrDefault();
    }

    private string ConvertAndCacheLevel(Parameter param)
    {
      if (param == null || param.StorageType != StorageType.ElementId)
      {
        return null;
      }

      return ConvertAndCacheLevel(param.AsElementId());
    }

    private string ConvertAndCacheLevel(ElementId id)
    {
      var level = Doc.GetElement(id) as DB.Level;
      //add it to our list of levels for the conversion so we can nest elements under them
      if (!Levels.ContainsKey(level.Name))
      {
        Levels[level.Name] = LevelToSpeckle(level);
      }

      return level.Name;
    }

    private DB.Level TryMatchExistingLevel(ILevel level)
    {
      var collector = new FilteredElementCollector(Doc).OfClass(typeof(DB.Level)).ToElements().Cast<DB.Level>();

      //match by name
      var revitLevel = collector.FirstOrDefault(x => x.Name == level.name);

      //match by id
      if (revitLevel == null && level is RevitLevel rl && !string.IsNullOrEmpty(rl.elementId))
      {
        revitLevel = collector.FirstOrDefault(x => x.Id.ToString() == rl.elementId);
      }

      //match by elevation
      if (revitLevel == null)
      {
        revitLevel = collector.FirstOrDefault(x => Math.Abs(x.Elevation - ScaleToNative((double)level.elevation, level.units)) < 0.1);
      }

      return revitLevel;
    }

    private RevitLevel LevelFromPoint(XYZ point)
    {
      return new RevitLevel() { elevation = ScaleToSpeckle(point.Z), name = "Speckle Level " + ScaleToSpeckle(point.Z) };
    }

    private RevitLevel LevelFromCurve(DB.Curve curve)
    {
      var start = curve.GetEndPoint(0);
      var end = curve.GetEndPoint(1);
      var point = start.Z < end.Z ? start : end; // pick the lowest
      return LevelFromPoint(point);
    }

    private RevitLevel EnsureLevelExists(RevitLevel level, XYZ point)
    {
      if (level != null)
      {
        return level;
      }

      return new RevitLevel() { elevation = ScaleToSpeckle(point.Z), name = "Speckle Level " + ScaleToSpeckle(point.Z) };
    }

    private RevitLevel EnsureLevelExists(RevitLevel level, DB.Curve curve)
    {
      if (level != null)
      {
        return level;
      }

      var point = curve.GetEndPoint(0);
      return EnsureLevelExists(level, point);
    }

    private RevitLevel EnsureLevelExists(RevitLevel level, object location)
    {
      if (level != null)
      {
        return level;
      }

      switch (location)
      {
        case XYZ point:
          return EnsureLevelExists(level, point);

        case DB.Curve curve:
          return EnsureLevelExists(level, curve);

        case DB.CurveArray curve:
          return EnsureLevelExists(level, curve.get_Item(0));

        default:
          throw new NotSupportedException();
      }
    }

  }
}