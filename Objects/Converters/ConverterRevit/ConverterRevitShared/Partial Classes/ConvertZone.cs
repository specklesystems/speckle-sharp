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

    public RevitZone ZoneToSpeckle(DB.Zone revitZone)
    {
      var speckleZone = new RevitZone
      {
        name = revitZone.Name,
        area = GetParamValue<double>(revitZone, BuiltInParameter.ROOM_AREA),
        volume = GetParamValue<double>(revitZone, BuiltInParameter.ROOM_VOLUME),
        perimeter = GetParamValue<double>(revitZone, BuiltInParameter.ZONE_PERIMETER),
        serviceType = revitZone.ServiceType.ToString()
      };

      GetAllRevitParamsAndIds(speckleZone, revitZone);

      // No implicit displayValue
      // speckleZone.displayValue = GetElementDisplayValue(revitSpace);

      speckleZone["phaseName"] = revitZone.Phase.Name;

      return speckleZone;
    }
  }
}
