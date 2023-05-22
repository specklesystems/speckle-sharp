using Autodesk.Revit.DB;
using Objects.BuiltElements;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB.Mechanical;
using Point = Objects.Geometry.Point;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject SpaceToNative(Space speckleSpace)
    {
      var revitSpace = GetExistingElementByApplicationId(speckleSpace.applicationId) as DB.Space;
      var appObj = new ApplicationObject(speckleSpace.id, speckleSpace.speckle_type) { applicationId = speckleSpace.applicationId };
      
      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(revitSpace, appObj))
        return appObj;

      var levelState = ApplicationObject.State.Unknown;
      var level = ConvertLevelToRevit(speckleSpace.level, out levelState);
      var basePoint = PointToNative(speckleSpace.basePoint);
      var upperLimit = ConvertLevelToRevit(speckleSpace.topLevel, out levelState);
      // create new space if none existing, include zone information if available
      if (revitSpace == null)
      {
        revitSpace = Doc.Create.NewSpace(level, new UV(basePoint.X, basePoint.Y));
        if (speckleSpace.zoneName != null)
        {
          var speckleZone = new FilteredElementCollector(Doc).OfClass(typeof(DB.Zone)).Cast<DB.Zone>().Where(z => z.Name == speckleSpace.zoneName).FirstOrDefault();
          if (speckleZone != null) // else space is added to default zone
          {
            var spaceSet = new DB.SpaceSet();
            spaceSet.Insert(revitSpace);
            speckleZone.AddSpaces(spaceSet);
          }
        }
      }
      else
      {
        // unplaced space, so just delete and recreate from scratch (but make sure to copy host zone info)
        // ideally we just place/move the existing space instead, but this seems like a real pain to do with the revit api? should update in future!
        if (revitSpace.Area == 0 && revitSpace.Location == null)
        {
          var zone = revitSpace.Zone;
          Doc.Delete(revitSpace.Id);
          revitSpace = Doc.Create.NewSpace(level, new UV(basePoint.X, basePoint.Y));
          if (zone != null)
          {
            var spaceSet = new DB.SpaceSet();
            spaceSet.Insert(revitSpace);
            zone.AddSpaces(spaceSet);
          }
        }
      }

      revitSpace.Name = speckleSpace.name;
      revitSpace.Number = speckleSpace.number;

      // if user does not specify an UpperLimit level, assume use of default UpperLimit (current level) and default offset values (base offset of 0, limit offset is distance to next level above)
      if (upperLimit != null)
      {
        TrySetParam(revitSpace, BuiltInParameter.ROOM_UPPER_LEVEL, upperLimit); // not sure why, don't actually seem to be able to set the UpperLimit property when UpperLimit is null and UpperLimit level to be provided is the same as the Level level - it stays null after assignment!
        revitSpace.LimitOffset = ScaleToNative(speckleSpace.topOffset, speckleSpace.units);
        revitSpace.BaseOffset = ScaleToNative(speckleSpace.baseOffset, speckleSpace.units);
      }

      if (!string.IsNullOrEmpty(speckleSpace.spaceType))
      {
        try
        {
          revitSpace.SpaceType = (DB.SpaceType)Enum.Parse(typeof(DB.SpaceType), speckleSpace.spaceType);
        }
        catch
        {
          revitSpace.SpaceType = DB.SpaceType.NoSpaceType;
        }
      }

      SetInstanceParameters(revitSpace, speckleSpace);
      appObj.Update(status: ApplicationObject.State.Created, createdId: revitSpace.UniqueId, convertedItem: revitSpace);
      return appObj;
    }

    public BuiltElements.Space SpaceToSpeckle(DB.Space revitSpace)
    {
      var profiles = GetProfiles(revitSpace);

      var speckleSpace = new Space();
      speckleSpace.name = revitSpace.Name;
      speckleSpace.number = revitSpace.Number;
      speckleSpace.basePoint = (Point)LocationToSpeckle(revitSpace);
      speckleSpace.level = ConvertAndCacheLevel(revitSpace.LevelId, revitSpace.Document);
      speckleSpace.topLevel = ConvertAndCacheLevel(revitSpace.get_Parameter(BuiltInParameter.ROOM_UPPER_LEVEL).AsElementId(), revitSpace.Document);
      speckleSpace.baseOffset = GetParamValue<double>(revitSpace, BuiltInParameter.ROOM_LOWER_OFFSET);
      speckleSpace.topOffset = GetParamValue<double>(revitSpace, BuiltInParameter.ROOM_UPPER_OFFSET);
      speckleSpace.outline = profiles.Count != 0 ? profiles[0] : null;
      if (profiles.Count > 1)
        speckleSpace.voids = profiles.Skip(1).ToList();
      speckleSpace.area = GetParamValue<double>(revitSpace, BuiltInParameter.ROOM_AREA);
      speckleSpace.volume = GetParamValue<double>(revitSpace, BuiltInParameter.ROOM_VOLUME);
      speckleSpace.spaceType = revitSpace.SpaceType.ToString();
      speckleSpace.zoneName = revitSpace.Zone?.Name;

      GetAllRevitParamsAndIds(speckleSpace, revitSpace);

      speckleSpace.displayValue = GetElementDisplayValue(revitSpace);

      return speckleSpace;
    }
  }
}
