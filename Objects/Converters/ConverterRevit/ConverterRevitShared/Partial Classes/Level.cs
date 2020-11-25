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

    public Level LevelToNative(ILevel speckleLevel)
    {
      var docLevels = new FilteredElementCollector(Doc).OfClass(typeof(DB.Level)).ToElements().Cast<DB.Level>();
      var existingLevelByName = docLevels.FirstOrDefault(docLevel => docLevel.Name == speckleLevel.name);
      var speckleLevelElevation = ScaleToNative((double)speckleLevel.elevation, ((Base)speckleLevel).units);

      switch (speckleLevel)
      {
        case RevitLevelByName revitLevelByName: // If it's a revit level by name, it's easy:
          if (existingLevelByName != null)
          {
            return existingLevelByName;
          }
          else
          {
            ConversionErrors.Add(new Error { message = $"Could not find level {speckleLevel.name} in this document. Please create it first, or use the RevitLevel class." });
            return null;
          }

        case RevitLevel revitLevel: // If it's a revit level proper:
          var existingLevelById = docLevels.FirstOrDefault(docLevel => docLevel.Id.ToString() == revitLevel.elementId);

          if (existingLevelByName == null && existingLevelById == null)
          {
            // create it
            var newLevel = Level.Create(Doc, speckleLevelElevation);
            newLevel.Name = revitLevel.name;

            if (revitLevel.createView)
            {
              CreateViewPlan(speckleLevel.name, newLevel.Id);
            }

            //SetElementParams(newLevel, revitLevel);
          }
          else
          {
            // edit it gently:
            // if elevations are different, edit it and cache the existing level as already modified. 
            // if we modify this level again, throw an error: "cannot have two levels with the same name and different elevations".

            var existingLevel = existingLevelByName == null ? existingLevelById : existingLevelByName; // consolidate the existing level.

            if (Math.Abs(speckleLevelElevation - existingLevel.Elevation) > 0.0328084)
            {
              if (modifiedLevels.ContainsKey(existingLevel.Name))
              {
                ConversionErrors.Add(new Error { details = $"Specifically, ${existingLevel.Name} has been found with an elevation of {speckleLevelElevation} and {modifiedLevels[speckleLevel.name].Elevation}.", message = $"Found levels with same name but different elevations." });

                return existingLevel;
              }

              existingLevel.Elevation = speckleLevelElevation;
              modifiedLevels[existingLevel.Name] = existingLevel;
            }

            return existingLevel;
          }
          break;

        case BuiltElements.Level level: // If it's a speckle level 
          if (existingLevelByName == null)
          {
            // create it
            var newLevel = Level.Create(Doc, speckleLevelElevation);
            newLevel.Name = level.name;

            return newLevel;
          }
          else
          {
            // edit it gently:
            // if elevations are different, edit it and cache the existing level as already modified. 
            // if we modify this level again, throw an error: "cannot have two levels with the same name and different elevations".

            if (Math.Abs(speckleLevelElevation - existingLevelByName.Elevation) > 0.0328084)
            {
              if (modifiedLevels.ContainsKey(existingLevelByName.Name))
              {
                ConversionErrors.Add(new Error { details = $"Specifically, ${existingLevelByName.Name} has been found with an elevation of {speckleLevelElevation} and {modifiedLevels[speckleLevel.name].Elevation}.", message = $"Found levels with same name but different elevations." });

                return existingLevelByName;
              }

              existingLevelByName.Elevation = speckleLevelElevation;
              modifiedLevels[existingLevelByName.Name] = existingLevelByName;
            }

            return existingLevelByName;
          }
      }

      // Sanity checks
      if (docLevels.Count() != 0)
      {
        ConversionErrors.Add(new Error { message = $"Did not know what to do with the speckle level ({speckleLevel.name} / {speckleLevel.elevation}). Returning first document level." });
        return docLevels.First();
      }
      else
      {
        ConversionErrors.Add(new Error { message = $"Document had no levels. Whoops! As well, did not know what to do with the speckle level ({speckleLevel.name} / {speckleLevel.elevation}). Creating a level at elevation 0." });
        return Level.Create(Doc, 0);
      }
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

      return Levels[level.Name];
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
        revitLevel = collector.FirstOrDefault(x => Math.Abs(x.Elevation - ScaleToNative((double)level.elevation, ((Base)level).units)) < 0.1);
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