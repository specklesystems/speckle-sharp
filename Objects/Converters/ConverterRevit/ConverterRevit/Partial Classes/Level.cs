using Autodesk.Revit.DB;
using Objects.Revit;
using System;
using System.Linq;

using DB = Autodesk.Revit.DB;

using Level = Objects.BuiltElements.Level;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public DB.Level LevelToNative(Level speckleLevel)
    {

      var (docObj, stateObj) = GetExistingElementByApplicationId(speckleLevel.applicationId, speckleLevel.speckle_type);

      //TODO: should check hashes on all conversions?
      // if the new and old have the same id (hash equivalent) and the doc obj is not marked as being modified, return the doc object
      if (stateObj != null && docObj != null && speckleLevel.id == stateObj.id && (bool)stateObj["userModified"] == false)
        return (DB.Level)docObj;

      if (docObj == null)
        docObj = TryMatchExistingLevel(speckleLevel);

      var elevation = speckleLevel.elevation * Scale;
      DB.Level revitLevel = null;

      //try update existing element
      if (docObj != null)
      {
        try
        {
          revitLevel = docObj as DB.Level;
          revitLevel.Elevation = elevation;
        }
        catch (Exception e)
        {
          //element update failed, create a new one
        }
      }

      var speckleRevitLevel = speckleLevel as RevitLevel;

      // create new element
      if (revitLevel == null)
      {
        revitLevel = DB.Level.Create(Doc, elevation);

        if (speckleRevitLevel != null && speckleRevitLevel.createView)
          CreateViewPlan(speckleLevel.name, revitLevel.Id);
      }

      //might fail if there's another level with the same name
      try
      {
        revitLevel.Name = speckleLevel.name;
      }
      catch { }

      if (speckleRevitLevel != null)
        SetElementParams(revitLevel, speckleRevitLevel);

      //revitLevel.Maximize3DExtents();
      return revitLevel;
    }

    public RevitLevel LevelToSpeckle(DB.Level revitLevel)
    {
      var speckleLevel = new RevitLevel();
      //TODO: check why using Scale?
      speckleLevel.elevation = revitLevel.Elevation / Scale; // UnitUtils.ConvertFromInternalUnits(myLevel.Elevation, DisplayUnitType.Meters)
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

    private DB.Level TryMatchExistingLevel(Level level)
    {
      var collector = new FilteredElementCollector(Doc).OfClass(typeof(DB.Level)).ToElements().Cast<DB.Level>();

      //match by name
      var revitLevel = collector.FirstOrDefault(x => x.Name == level.name);
      //match by id
      if (revitLevel == null && level.HasMember<string>("elementId"))
        revitLevel = collector.FirstOrDefault(x => x.Id.ToString() == level.GetMemberSafe("elementId", ""));
      //match by elevation
      if (revitLevel == null)
        revitLevel = collector.FirstOrDefault(x => Math.Abs(x.Elevation - level.elevation * Scale) < 0.1);

      return revitLevel;
    }

    private RevitLevel LevelFromPoint(XYZ point)
    {
      return new RevitLevel() { elevation = point.Z / Scale, name = "Speckle Level " + point.Z / Scale };
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
        return level;

      return new RevitLevel() { elevation = point.Z / Scale, name = "Speckle Level " + point.Z / Scale };
    }

    private RevitLevel EnsureLevelExists(RevitLevel level, DB.Curve curve)
    {
      if (level != null)
        return level;

      var point = curve.GetEndPoint(0);
      return EnsureLevelExists(level, point);
    }

    private RevitLevel EnsureLevelExists(RevitLevel level, object location)
    {
      if (level != null)
        return level;

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