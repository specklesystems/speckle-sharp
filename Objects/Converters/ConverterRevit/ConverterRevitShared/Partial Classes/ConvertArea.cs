using System.Linq;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using Point = Objects.Geometry.Point;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    //public List<ApplicationObject> AreaToNative(BuiltElements.Area speckleArea)
    //{
    //  var revitRoom = GetExistingElementByApplicationId(speckleArea.applicationId) as DB.Area;
    //  var level = LevelToNative(speckleArea.level);


    //  //TODO: support updating rooms
    //  if (revitRoom != null)
    //  {
    //    Doc.Delete(revitRoom.Id);
    //  }

    //  revitRoom = Doc.Create.NewArea(level, new UV(speckleArea.center.x, speckleArea.center.y));

    //  revitRoom.Name = speckleArea.name;
    //  revitRoom.Number = speckleArea.number;

    //  SetInstanceParameters(revitRoom, speckleArea);

    //  var placeholders = new List<ApplicationObject>()
    //  {
    //    new ApplicationObject
    //    {
    //    applicationId = speckleArea.applicationId,
    //    ApplicationGeneratedId = revitRoom.UniqueId,
    //    NativeObject = revitRoom
    //    }
    //  };

    //  return placeholders;

    //}

    public BuiltElements.Area AreaToSpeckle(DB.Area revitArea)
    {
      var profiles = GetProfiles(revitArea);

      var speckleArea = new BuiltElements.Area();

      speckleArea.name = revitArea.get_Parameter(BuiltInParameter.ROOM_NAME).AsString();
      speckleArea.number = revitArea.Number;
      speckleArea.center = (Point)LocationToSpeckle(revitArea);
      speckleArea.level = ConvertAndCacheLevel(revitArea, BuiltInParameter.ROOM_LEVEL_ID);
      if (profiles.Any())
        speckleArea.outline = profiles[0];
      speckleArea.area = GetParamValue<double>(revitArea, BuiltInParameter.ROOM_AREA);
      if (profiles.Count > 1)
        speckleArea.voids = profiles.Skip(1).ToList();

      GetAllRevitParamsAndIds(speckleArea, revitArea);

      //no mesh seems to be retriavable, not even using the SpatialElementGeometryCalculator 
      //speckleArea.displayValue = GetElementDisplayValue(revitArea);
      return speckleArea;
    }
  }
}
