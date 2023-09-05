using System;
using System.Linq;
using Autodesk.Revit.DB;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB.Mechanical;
using Level = Autodesk.Revit.DB.Level;

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

      if (speckleZone == null)
        return null;

      var revitZone = GetExistingZone(speckleZone);

      var targetPhase = DetermineTargetPhase(speckleZone, revitZone);

      var revitZoneLevel = ConvertLevelToRevit(speckleZone.level, out _);
      if (revitZone == null)
      {
        DB.Zone sameNameZone = GetZoneWithSameName(speckleZone);

        if (sameNameZone != null && sameNameZone.Phase.Name == speckleZone.phaseName)
        {
          revitZone = sameNameZone;
        }
        else
        {
          if (sameNameZone != null)
          {
            Doc.Delete(sameNameZone.Id);
            Doc.Regenerate();
          }

          revitZone = CreateRevitZone(revitZoneLevel, targetPhase, speckleZone.name);
        }
      }
      else
      {
        revitZone.Name = speckleZone.name;
      }

      SetInstanceParameters(revitZone, speckleZone);

      var getSameNameZone = GetZoneWithSameName(speckleZone);

      state = ApplicationObject.State.Updated;

      return revitZone;
    }

    private DB.Zone GetExistingZone(RevitZone speckleZone)
    {
      return GetExistingElementByApplicationId(speckleZone.applicationId) as DB.Zone;
    }

    private DB.Zone GetZoneWithSameName(RevitZone speckleZone)
    {
      var docZones = new FilteredElementCollector(Doc).OfClass(typeof(DB.Zone)).ToElements().Cast<DB.Zone>();

      return docZones.FirstOrDefault(x => x.Name == speckleZone.name);
    }

    public DB.Zone CreateRevitZone(Level revitZoneLevel, Phase targetPhase, string zoneName = null)
    {
      var newZone = Doc.Create.NewZone(revitZoneLevel, targetPhase);

      if (zoneName != null)
        newZone.Name = zoneName;
      Doc.Regenerate();
      return newZone;
    }

    /// <summary>
    /// Determines the target phase for a space based on available information.
    /// </summary>
    /// <param name="speckleZone">The Space object from the Speckle system.</param>
    /// <param name="revitSpace">The Space object from the Revit system.</param>
    /// <returns>The determined target Phase object.</returns>
    /// <remarks>
    /// The method tries to determine the target phase based on the following priority:
    /// 1. Phase from the Speckle space (if it exists).
    /// 2. Phase from the existing Revit space (if it exists).
    /// 3. Phase from the active view in Revit.
    /// </remarks>
    private Phase DetermineTargetPhase(RevitZone speckleZone, DB.Zone revitZone)
    {
      // Get all phases
      var phases = Doc.Phases.Cast<Phase>();

      // Determine existing space phase, if any
      Phase existingZonePhase = null;
      if (revitZone != null)
      {
        string existingZonePhaseName = revitZone.Phase.ToString();
        existingZonePhase = phases.FirstOrDefault(x => x.Name == existingZonePhaseName);
      }

      // Determine active view phase
      string activeViewPhaseName = Doc.ActiveView.get_Parameter(BuiltInParameter.VIEW_PHASE).AsValueString();
      var activeViewPhase = phases.FirstOrDefault(x => x.Name == activeViewPhaseName);

      // Determine target phase
      // Priority: speckleSpace phase > existing space phase > active view phase
      string targetPhaseName = speckleZone.phaseName;
      var targetPhase = phases.FirstOrDefault(x => x.Name == targetPhaseName) ?? existingZonePhase ?? activeViewPhase;

      return targetPhase;
    }

    /// <summary>
    /// Handles the Revit Zone based on the provided Speckle Space and target phase.
    /// </summary>
    /// <param name="speckleSpace">The Space object from the Speckle system.</param>
    /// <param name="revitZone">The existing Revit Zone object. This may be modified by the method.</param>
    /// <param name="targetPhase">The target Phase object.</param>
    /// <param name="level">The Level object associated with the space.</param>
    /// <returns>The modified or newly created Revit Zone object.</returns>
    private DB.Zone CreateRevitZoneIfNeeded(Space speckleSpace, DB.Zone revitZone, Phase targetPhase, Level level)
    {
      var zoneName = speckleSpace.zone != null ? speckleSpace.zone.name : speckleSpace.zoneName; // zoneName is the previous property retained here for backwards compatibility.

      if (revitZone == null && !string.IsNullOrEmpty(zoneName))
      {
        revitZone = ConvertZoneToRevit(speckleSpace.zone, out _);
      }
      else if (revitZone != null && revitZone.Phase.Name != targetPhase.Name)
      {
        Doc.Delete(revitZone.Id);

        revitZone = string.IsNullOrEmpty(speckleSpace.zone.name)
          ? ConvertZoneToRevit(speckleSpace.zone, out _)
          : CreateRevitZone(level, targetPhase);

        revitZone.Name = speckleSpace.zone.name;
      }

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

      var level = revitZone.Spaces.Cast<DB.Space>().Select(x => x.Level).First();

      if (level != null)
        speckleZone.level = LevelToSpeckle(level);

      GetAllRevitParamsAndIds(speckleZone, revitZone);

      // No implicit displayValue
      // speckleZone.displayValue = GetElementDisplayValue(revitSpace);

      speckleZone["phaseName"] = revitZone.Phase.Name;

      return speckleZone;
    }
  }
}
