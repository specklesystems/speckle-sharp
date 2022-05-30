using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Objects.Organization;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    /// <summary>
    /// Convert a Speckle <see cref="Organization.Level"/> to Revit.
    /// This finds or creates a Revit level based on <see cref="Organization.Level.elevation"/> only unless this
    /// <see cref="Organization.Level"/> is marked as <see cref="Organization.Level.referenceOnly"/>. In this case, a
    /// match by <see cref="Organization.Level.name"/> will be attempted. If a match is not found, a new level at the
    /// given <see cref="Organization.Level.elevation"/> will be created.
    /// </summary>
    /// <param name="speckleLevel"></param>
    /// <returns></returns>
    public DB.Level ConvertLevelToRevit(Organization.Level speckleLevel)
    {
      if ( speckleLevel == null ) return null;
      
      var docLevels = new FilteredElementCollector(Doc).OfClass(typeof(DB.Level)).ToElements().Cast<DB.Level>();
      var tryElevationMatch = true;

      DB.Level nameMatch = docLevels.FirstOrDefault(x => x.Name == speckleLevel.name);
      DB.Level elevationMatch = null;

      if ( speckleLevel.referenceOnly )
      {
        //see: https://speckle.community/t/revit-connector-levels-and-spaces/2824/5
        tryElevationMatch = false;
        if ( nameMatch != null )
          return nameMatch;
      }

      var speckleLevelElevation = ScaleToNative(speckleLevel.elevation, speckleLevel.units);
      
      if ( tryElevationMatch )
        elevationMatch =
          docLevels.FirstOrDefault(l => Math.Abs(l.Elevation - speckleLevelElevation) < TOLERANCE);

      //a level that had been previously received or a level with matching elevation
      var revitLevel = GetExistingElementByApplicationId(speckleLevel.applicationId) as DB.Level ?? elevationMatch;

      //the level has been received before (via schema builder probably)
      //match by appid => update level
      if ( revitLevel != null )
      {
        //update name
        if ( nameMatch == null )
          revitLevel.Name = speckleLevel.name;

        if ( elevationMatch == null || Math.Abs(revitLevel.Elevation - speckleLevelElevation) >= TOLERANCE )
          revitLevel.Elevation = speckleLevelElevation;

        Report.Log($"Updated Level {revitLevel.Name} {revitLevel.Id}");
      }
      // If we don't have an existing level, create it.
      else
      {
        revitLevel = DB.Level.Create(Doc, speckleLevelElevation);
        if ( nameMatch == null )
          revitLevel.Name = speckleLevel.name;
        if ( speckleLevel.sourceApp is RevitLevelProperties { createView: true }  )
        {
          CreateViewPlan(speckleLevel.name, revitLevel.Id);
        }

        Report.Log($"Created Level {revitLevel.Name} {revitLevel.Id}");
      }
      
      return revitLevel;
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
    public DB.Level ConvertLevelToRevit(BuiltElements.Level speckleLevel)
    {
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
      DB.Level existingLevelWithSameElevation = null;
      if (elevationMatch)
        docLevels.FirstOrDefault(l => Math.Abs(l.Elevation - (double)speckleLevelElevation) < TOLERANCE);

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
        {
          revitLevel.Elevation = speckleLevelElevation;
        }

        Report.Log($"Updated Level {revitLevel.Name} {revitLevel.Id}");
      }
      //match by elevation
      else if (existingLevelWithSameElevation != null)
      {
        revitLevel = existingLevelWithSameElevation;
        if (!hasLevelWithSameName)
        {
          revitLevel.Name = speckleLevel.name;
          Report.Log($"Updated Level {revitLevel.Name} {revitLevel.Id}");
        }
      }

      else
      {
        // If we don't have an existing level, create it.
        revitLevel = DB.Level.Create(Doc, (double)speckleLevelElevation);
        if (!hasLevelWithSameName)
          revitLevel.Name = speckleLevel.name;
        var rl = speckleLevel as RevitLevel;
        if (rl != null && rl.createView)
        {
          CreateViewPlan(speckleLevel.name, revitLevel.Id);
        }

        Report.Log($"Created Level {revitLevel.Name} {revitLevel.Id}");
      }


      return revitLevel;

    }

    public List<ApplicationPlaceholderObject> LevelToNative(BuiltElements.Level speckleLevel)
    {
      var revitLevel = ConvertLevelToRevit(speckleLevel);
      var placeholders = new List<ApplicationPlaceholderObject>() { new ApplicationPlaceholderObject { applicationId = speckleLevel.applicationId, ApplicationGeneratedId = revitLevel.UniqueId, NativeObject = revitLevel } };

      return placeholders;
    }

    public RevitLevel LevelToSpeckle(DB.Level revitLevel)
    {
      var speckleLevel = new RevitLevel();

      speckleLevel.elevation = ScaleToSpeckle(revitLevel.Elevation);
      speckleLevel.name = revitLevel.Name;
      speckleLevel.createView = true;

      GetAllRevitParamsAndIds(speckleLevel, revitLevel);

      Report.Log($"Converted Level {revitLevel.Id}");
      return speckleLevel;
    }

    public Organization.Level Level2ToSpeckle(DB.Level revitLevel)
    {
      var speckleLevel = new Organization.Level
      {
        units = ModelUnits,
        name = revitLevel.Name,
        applicationId = revitLevel.UniqueId,
        elevation = ScaleToSpeckle(revitLevel.Elevation)
      };

      speckleLevel.sourceApp = new RevitLevelProperties()
      {
        createView = true,
        elementId = revitLevel.Id.ToString(),
        props = GetRevitParams(speckleLevel, revitLevel)
      };

      Report.Log($"Converted Level {revitLevel.Id}");
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

      Report.Log($"Created ViewPlan {view.Id}");
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

      Report.LogConversionError(new Exception($"Could not find level `{name}`, a default level will be used."));

      return collector.FirstOrDefault();
    }

    private RevitLevel ConvertAndCacheLevel(DB.Element elem, BuiltInParameter bip)
    {
      var param = elem.get_Parameter(bip);

      if (param == null || param.StorageType != StorageType.ElementId)
      {
        return null;
      }

      return ConvertAndCacheLevel(param.AsElementId(), elem.Document);
    }

    private RevitLevel ConvertAndCacheLevel(ElementId id, Document doc)
    {
      var level = doc.GetElement(id) as DB.Level;

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

    /// <summary>
    /// Gets a Revit <see cref="DB.Level"/> by <see cref="BuiltInParameter"/> and converts it to a Speckle <see cref="Organization.Level"/>. 
    /// This will also cache the converted <see cref="Organization.Level"/> by saving it to the current <see cref="ConverterRevit"/>.
    /// </summary>
    /// <param name="elem">The Revit <see cref="DB.Element"/> from which to get the <see cref="DB.Level"/> to convert</param>
    /// <param name="bip">The <see cref="BuiltInParameter"/> that correlates to the <see cref="DB.Level"/> to convert</param>
    /// <returns>The resulting Speckle <see cref="Organization.Level"/></returns>
    private Organization.Level ConvertAndCacheLevel2(DB.Element elem, BuiltInParameter bip)
    {
      var param = elem.get_Parameter(bip);

      if ( !( param is { StorageType: StorageType.ElementId } ) )
        return null;

      return ConvertAndCacheLevel2(param.AsElementId(), elem.Document);
    }
    
    /// <summary>
    /// Gets a Revit <see cref="DB.Level"/> by <see cref="DB.ElementId"/> and converts it to a Speckle <see cref="Organization.Level"/>. 
    /// This will also cache the converted <see cref="Organization.Level"/> by saving it to the current <see cref="ConverterRevit"/>.
    /// </summary>
    /// <param name="id">The <see cref="DB.ElementId"/> of the Revit <see cref="DB.Level"/> to convert</param>
    /// <param name="doc">The <see cref="Document"/> containing the Revit <see cref="DB.Level"/> to convert</param>
    /// <returns>The resulting Speckle <see cref="Organization.Level"/></returns>
    private Organization.Level ConvertAndCacheLevel2(ElementId id, Document doc)
    {
      var level = doc.GetElement(id) as DB.Level;

      if ( level == null ) return null;
      if ( !Levels2.ContainsKey(level.Name) )
      {
        Levels2[ level.Name ] = Level2ToSpeckle(level);
      }

      return Levels2[ level.Name ];
    }

    private Organization.Level Level2FromPoint(XYZ point)
    {
      var p = PointToSpeckle(point);
      return new Organization.Level { elevation = p.z, name = "Generated Level " + p.z, units = ModelUnits };
    }

    private Organization.Level Level2FromCurve(Curve curve)
    {
      var start = curve.GetEndPoint(0);
      var end = curve.GetEndPoint(1);
      var point = start.Z < end.Z ? start : end; // pick the lowest
      return Level2FromPoint(point);
    }

    private DB.Level GetFirstDocLevel()
    {
      var docLevels = new FilteredElementCollector(Doc).OfClass(typeof(DB.Level)).ToElements().Cast<DB.Level>();
      return docLevels.First();
    }
  }
}