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

    private Dictionary<string, Level> modifiedLevels = new Dictionary<string, Level>();

    /// <summary>
    /// BORKED: what happens if you change the level's name?
    /// </summary>
    /// <param name="speckleLevel"></param>
    /// <returns></returns>
    public Level LevelToNative(BuiltElements.Level speckleLevel)
    {
      var docLevels = new FilteredElementCollector(Doc).OfClass(typeof(DB.Level)).ToElements().Cast<DB.Level>();

      var existingLevel = docLevels.FirstOrDefault(docLevel => docLevel.Name == speckleLevel.name);
      var speckleLevelElevation = speckleLevel.elevation != null ? (double?)ScaleToNative((double)speckleLevel.elevation, ((Base)speckleLevel).units) : null;

      // If we don't have an elevation, either return the existing level or fail.
      if (speckleLevelElevation == null)
      {
        if (existingLevel != null)
        {
          return existingLevel;
        }
        else
        {
          ConversionErrors.Add(new Error { message = $"Could not find level {speckleLevel.name} in this document. Please create it first by setting an elevation." });
          return null;
        }
      }

      // If we don't have an existing level, create it.
      if (existingLevel == null)
      {
        var existingLevelByElevation = docLevels.FirstOrDefault(l => Math.Abs(l.Elevation - (double)speckleLevelElevation) < 0.0164042);
        if (existingLevelByElevation != null)
        {
          existingLevel = existingLevelByElevation;
        }
        else
        {
          existingLevel = Level.Create(Doc, (double)speckleLevelElevation);
          existingLevel.Name = speckleLevel.name;
        }

        if (speckleLevel is RevitLevel rl && rl.createView)
        {
          CreateViewPlan(speckleLevel.name, existingLevel.Id);
        }
        return existingLevel;
      }

      // If we do have an existing level and the elevations are different, gently edit the existing level.
      if (Math.Abs((double)speckleLevelElevation - existingLevel.Elevation) > 0.0164042) // 0.5cm tolerance.
      {
        if (modifiedLevels.ContainsKey(existingLevel.Name))
        {
          ConversionErrors.Add(new Error { details = $"Specifically, ${existingLevel.Name} has been found with an elevation of {speckleLevelElevation} and {modifiedLevels[speckleLevel.name].Elevation}.", message = $"Found levels with same name but different elevations." });

          return existingLevel;
        }

        existingLevel.Elevation = (double)speckleLevelElevation;
        modifiedLevels[existingLevel.Name] = existingLevel;
        return existingLevel;
      }

      return existingLevel;
    }

    public RevitLevel LevelToSpeckle(DB.Level revitLevel)
    {
      var speckleLevel = new RevitLevel();

      speckleLevel.elevation = ScaleToSpeckle(revitLevel.Elevation);
      speckleLevel.name = revitLevel.Name;
      speckleLevel.createView = true;

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

      ConversionErrors.Add(new Error($"Could not find level `{name}`", "A default level will be used."));

      return collector.FirstOrDefault();
    }

    private RevitLevel ConvertAndCacheLevel(Parameter param)
    {
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
      //add it to our list of levels for the conversion so we can nest elements under them
      if (!Levels.ContainsKey(level.Name))
      {
        Levels[level.Name] = LevelToSpeckle(level);
      }

      return Levels[level.Name] as RevitLevel;
    }

    private RevitLevel LevelFromPoint(XYZ point)
    {
      return new RevitLevel() { elevation = ScaleToSpeckle(point.Z), name = "Speckle Level " + ScaleToSpeckle(point.Z), units = ModelUnits };
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