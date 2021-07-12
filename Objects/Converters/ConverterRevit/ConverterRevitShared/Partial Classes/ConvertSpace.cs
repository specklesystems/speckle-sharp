using Autodesk.Revit.DB;
using Objects.BuiltElements;
using Objects.Geometry;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
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


            //TODO: support updating rooms
            if (revitSpace != null)
            {
                Doc.Delete(revitSpace.Id);
            }

            revitSpace = Doc.Create.NewSpace(level, new UV(speckleSpace.basePoint.x, speckleSpace.basePoint.y));

            revitSpace.Name = speckleSpace.name;
            revitSpace.Number = speckleSpace.number;

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
            speckleSpace.baseOffset = GetParamValue<double>(revitSpace, BuiltInParameter.ROOM_LOWER_OFFSET);
            speckleSpace.upperLimit = ConvertAndCacheLevel(revitSpace.UpperLimit.Id);
            speckleSpace.limitOffset = GetParamValue<double>(revitSpace, BuiltInParameter.ROOM_UPPER_OFFSET);
            speckleSpace.boundary = profiles[0];
            speckleSpace.area = GetParamValue<double>(revitSpace, BuiltInParameter.ROOM_AREA);
            speckleSpace.volume = GetParamValue<double>(revitSpace, BuiltInParameter.ROOM_VOLUME);

            //speckleSpace.baseOffset = revitSpace.BaseOffset;
            //speckleSpace.limitOffset = revitSpace.LimitOffset;
            //speckleSpace.area = revitSpace.Area;
            //speckleSpace.volume = revitSpace.Volume;

            GetAllRevitParamsAndIds(speckleSpace, revitSpace);
            speckleSpace.displayMesh = GetElementDisplayMesh(revitSpace);

            return speckleSpace;
        }




    }
}