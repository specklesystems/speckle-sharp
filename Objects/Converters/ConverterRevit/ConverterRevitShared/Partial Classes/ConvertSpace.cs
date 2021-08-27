using Autodesk.Revit.DB;
using Objects.BuiltElements;
using Objects.Geometry;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using DB = Autodesk.Revit.DB.Mechanical;
using Point = Objects.Geometry.Point;

namespace Objects.Converter.Revit
{
    public partial class ConverterRevit
    {
        public List<ApplicationPlaceholderObject> SpaceToNative(Space speckleSpace)
        {
            var revitSpace = GetExistingElementByApplicationId(speckleSpace.applicationId) as DB.Space;
            var level = LevelToNative(speckleSpace.level);
            var basePoint = PointToNative(speckleSpace.basePoint);
            var upperLimit = LevelToNative(speckleSpace.upperLimit);

            // create new space if none existing, include zone information if available
            if (revitSpace == null)
            {                
                revitSpace = Doc.Create.NewSpace(level, new UV(basePoint.X, basePoint.Y));
                if (speckleSpace.zoneId != null)
                {
                    var speckleZone = Doc.GetElement(speckleSpace.zoneId) as DB.Zone;
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
            if(upperLimit != null)
            {
                revitSpace.get_Parameter(BuiltInParameter.ROOM_UPPER_LEVEL).Set(upperLimit.Id); // not sure why, don't actually seem to be able to set the UpperLimit property when UpperLimit is the same as Level - it stays null after assignment!
                revitSpace.LimitOffset = ScaleToNative(speckleSpace.limitOffset, speckleSpace.units);
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

            var placeholders = new List<ApplicationPlaceholderObject>()
              {
                new ApplicationPlaceholderObject
                {
                applicationId = speckleSpace.applicationId,
                ApplicationGeneratedId = revitSpace.UniqueId,
                NativeObject = revitSpace
                }
              };

            return placeholders;
        }

        public BuiltElements.Space SpaceToSpeckle(DB.Space revitSpace)
        {
            var profiles = GetProfiles(revitSpace);

            var speckleSpace = new Space();
            speckleSpace.name = revitSpace.Name;
            speckleSpace.number = revitSpace.Number;
            speckleSpace.basePoint = (Point)LocationToSpeckle(revitSpace);
            speckleSpace.level = ConvertAndCacheLevel(revitSpace.LevelId);
            speckleSpace.upperLimit = ConvertAndCacheLevel(revitSpace.get_Parameter(BuiltInParameter.ROOM_UPPER_LEVEL).AsElementId());
            speckleSpace.baseOffset = GetParamValue<double>(revitSpace, BuiltInParameter.ROOM_LOWER_OFFSET); 
            speckleSpace.limitOffset = GetParamValue<double>(revitSpace, BuiltInParameter.ROOM_UPPER_OFFSET);
            speckleSpace.boundary = profiles[0];
            speckleSpace.area = GetParamValue<double>(revitSpace, BuiltInParameter.ROOM_AREA);
            speckleSpace.volume = GetParamValue<double>(revitSpace, BuiltInParameter.ROOM_VOLUME);
            speckleSpace.spaceType = revitSpace.SpaceType.ToString();
            speckleSpace.zoneId = revitSpace.Zone.Id.ToString();

            GetAllRevitParamsAndIds(speckleSpace, revitSpace);
            speckleSpace.displayMesh = GetElementDisplayMesh(revitSpace);

            return speckleSpace;
        }
    }
}