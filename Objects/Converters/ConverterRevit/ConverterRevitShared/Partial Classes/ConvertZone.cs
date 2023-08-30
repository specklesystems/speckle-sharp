using System;
using System.Linq;
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB.Mechanical;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject ZoneToNative(RevitZone speckleZone)
    {
      var revitZone = ConvertZoneToRevit(speckleZone, out ApplicationObject.State state);
      var appObj = new ApplicationObject(speckleZone.id, speckleZone.speckle_type)
      {
        applicationId = speckleZone.applicationId
      };
      appObj.Update(status: state, createdId: revitZone.UniqueId, convertedItem: revitZone);
      return appObj;
    }

    public DB.Zone ConvertZoneToRevit(RevitZone speckleZone, out ApplicationObject.State state)
    {
      state = ApplicationObject.State.Unknown;

      var docZones = new FilteredElementCollector(Doc).OfClass(typeof(DB.Zone)).ToElements().Cast<DB.Zone>();

      if (speckleZone == null)
        return null;

      var hasZoneWithSameName = docZones.Any(x => x.Name == speckleZone.name);

      DB.Zone existingZone = null;

      // a zone that had been previously received
      var revitZone = GetExistingElementByApplicationId(speckleZone.applicationId) as DB.Zone;

      var revitZoneLevel = ConvertLevelToRevit(speckleZone.level, out _);

      var activePhase = Doc.Phases
        .Cast<Phase>()
        .FirstOrDefault(x => x.Name == Doc.ActiveView.get_Parameter(BuiltInParameter.VIEW_PHASE).AsValueString());

      // Does this zone phase exist on the document?
      var targetPhase = Doc.Phases.Cast<Phase>().FirstOrDefault(x => x.Name == speckleZone["phaseName"]);

      if (targetPhase == null)
      {
        // We're out of options, API doesn't allow us to create a Phase.
        Report.LogConversionError(
          new Exception($"Could not find phase `{speckleZone["phaseName"]}`, a default phase will be used.")
        );
        targetPhase = activePhase;
      }

      if (revitZone != null)
      {
        if (!hasZoneWithSameName)
          revitZone.Name = speckleZone.name;

        if (revitZone.Phase != null)
        {
          if (revitZone.Phase.Name != speckleZone["phaseName"])
          {
            // we'll have to delete the existing Zone and Make a new one.
            revitZone = Doc.Create.NewZone(revitZoneLevel, targetPhase);
            revitZone.Name = speckleZone.name;
          }

          SetInstanceParameters(revitZone, speckleZone);
        }
        else
        {
          revitZone = Doc.Create.NewZone(revitZoneLevel, targetPhase);
          revitZone.Name = speckleZone.name;
        }

        state = ApplicationObject.State.Updated;
      }

      state = ApplicationObject.State.Created;

      return revitZone;
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
