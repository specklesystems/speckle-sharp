using Autodesk.Revit.DB;
using Objects.BuiltElements;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Objects.BuiltElements.Revit;
using DB = Autodesk.Revit.DB.Mechanical;
using Point = Objects.Geometry.Point;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject ZoneToNative(Zone speckleZone)
    {
      var revitZone = GetExistingElementByApplicationId(speckleZone.applicationId) as DB.Zone;
      var appObj = new ApplicationObject(speckleZone.id, speckleZone.speckle_type)
      {
        applicationId = speckleZone.applicationId
      };

      return appObj;
    }

    public BuiltElements.Revit.Zone ZoneToSpeckle(DB.Zone revitZone)
    {
      var speckleZone = new Zone
      {
        name = revitZone.Name,
        area = GetParamValue<double>(revitZone, BuiltInParameter.ROOM_AREA),
        volume = GetParamValue<double>(revitZone, BuiltInParameter.ROOM_VOLUME),
        perimeter = GetParamValue<double>(revitZone, BuiltInParameter.ZONE_PERIMETER),
        serviceType = revitZone.ServiceType.ToString()
      };

      // Optionally Zones and Spaces could be sent as a Parent Child, but edge cases abound.
      // var spaces = new List<DB.Space>();
      // var zoneSpaces = revitZone.Spaces.GetEnumerator();
      //
      // while (zoneSpaces.MoveNext())
      // {
      //   if (zoneSpaces.Current is DB.Space space)
      //   {
      //     spaces.Add(space);
      //   }
      // }
      // speckleZone.spaces = spaces.Select(x => SpaceToSpeckle(x)).ToList();

      GetAllRevitParamsAndIds(speckleZone, revitZone);

      // No implicit displayValue
      // speckleZone.displayValue = GetElementDisplayValue(revitSpace);

      return speckleZone;
    }
  }
}
