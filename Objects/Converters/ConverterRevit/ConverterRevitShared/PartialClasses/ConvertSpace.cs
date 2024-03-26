using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB.Mechanical;
using Level = Autodesk.Revit.DB.Level;
using Point = Objects.Geometry.Point;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  public ApplicationObject SpaceToNative(Space speckleSpace)
  {
    var appObj = new ApplicationObject(speckleSpace.id, speckleSpace.speckle_type)
    {
      applicationId = speckleSpace.applicationId
    };

    var revitSpace = GetExistingElementByApplicationId(speckleSpace.applicationId) as DB.Space;

    // skip if element already exists in doc & receive mode is set to ignore
    if (IsIgnore(revitSpace, appObj))
    {
      return appObj;
    }

    // Determine Space Location
    if (speckleSpace.basePoint is null)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "Space Base Point was null");
      return appObj;
    }

    var level = ConvertLevelToRevit(speckleSpace.level, out _);
    var basePoint = PointToNative(speckleSpace.basePoint);
    var upperLimit = ConvertLevelToRevit(speckleSpace.topLevel, out _);

    // no target phase found, it will use the active view phase anyway
    var targetPhase = DetermineTargetPhase(speckleSpace, revitSpace);
    var activeViewPhase = DetermineActiveViewPhase();

    // even though the API documented to allow for spaces to be created in any phase,
    // it is not possible to create on any phase but the Active View phase
    var activeViewPhaseName = activeViewPhase?.Name;
    var targetPhaseName = targetPhase?.Name;

    if (activeViewPhase.Id != targetPhase.Id)
    {
      appObj.Update(
        logItem: $"Space Phase {targetPhase.Name} not selected in the Active View.",
        status: ApplicationObject.State.Skipped
      );
      return appObj;
      // return null;
    }

    revitSpace = CreateRevitSpaceIfNeeded(speckleSpace, revitSpace, targetPhase, level, basePoint);

    if (revitSpace == null)
    {
      appObj.Update(status: ApplicationObject.State.Failed);
      return appObj;
    }

    var revitZone = revitSpace.Zone;
    revitZone = CreateRevitZoneIfNeeded(speckleSpace, revitZone, targetPhase, level);

    // if a relevant zone exists add space to it
    if (revitZone != null)
    {
      var currentSpaces = revitZone.Spaces;

      // if the space is already in the zone, do nothing.
      if (!currentSpaces.Contains(revitSpace))
      {
        var spaceSet = new DB.SpaceSet();
        spaceSet.Insert(revitSpace);
        revitZone.AddSpaces(spaceSet);
      }
    }

    revitSpace.Name = speckleSpace.name;
    revitSpace.Number = speckleSpace.number;

    SetSpaceLimits(speckleSpace, upperLimit, revitSpace);
    SetSpaceType(speckleSpace, revitSpace);
    SetInstanceParameters(revitSpace, speckleSpace);

    appObj.Update(status: ApplicationObject.State.Created, createdId: revitSpace.UniqueId, convertedItem: revitSpace);
    return appObj;
  }

  /// <summary>
  /// Sets the type of the Revit space based on the provided Speckle space.
  /// </summary>
  /// <param name="speckleSpace">The Space object from the Speckle system.</param>
  /// <param name="revitSpace">The Revit Space object to update.</param>
  /// <remarks>
  /// The method will try to set the Revit space type based on the Speckle space type.
  /// If the Speckle space type is not recognized, it will default to 'NoSpaceType'.
  /// </remarks>
  private static void SetSpaceType(Space speckleSpace, DB.Space revitSpace)
  {
    if (string.IsNullOrEmpty(speckleSpace.spaceType))
    {
      return;
    }

    revitSpace.SpaceType = Enum.TryParse(speckleSpace.spaceType, out DB.SpaceType spaceType)
      ? spaceType
      : DB.SpaceType.NoSpaceType;
  }

  /// <summary>
  /// Sets the upper limit and offsets for a Revit space based on a Speckle space.
  /// </summary>
  /// <param name="speckleSpace">The Speckle Space object containing the upper limit and offset information.</param>
  /// <param name="upperLimit">The Element object representing the upper limit level.</param>
  /// <param name="revitSpace">The Revit Space object to be updated.</param>
  /// <remarks>
  /// The method will attempt to set the upper limit and offsets for the Revit space.
  /// If an upper limit is not specified, the method will return early without making changes.
  /// </remarks>
  private void SetSpaceLimits(Space speckleSpace, Element upperLimit, DB.Space revitSpace)
  {
    if (upperLimit == null)
    {
      return;
    }

    TrySetParam(revitSpace, BuiltInParameter.ROOM_UPPER_LEVEL, upperLimit);
    TrySetParam(
      revitSpace,
      BuiltInParameter.ROOM_UPPER_OFFSET,
      ScaleToNative(speckleSpace.topOffset, speckleSpace.units)
    );
    TrySetParam(
      revitSpace,
      BuiltInParameter.ROOM_LOWER_OFFSET,
      ScaleToNative(speckleSpace.baseOffset, speckleSpace.units)
    );
  }

  /// <summary>
  /// Handles the Revit Space based on the provided Speckle Space and target phase.
  /// </summary>
  /// <param name="speckleSpace">The Space object from the Speckle system.</param>
  /// <param name="revitSpace">The existing Revit Space object. This may be modified by the method.</param>
  /// <param name="targetPhase">The target Phase object.</param>
  /// <param name="level">The Level object associated with the space.</param>
  /// <param name="basePoint">The base point for the space.</param>
  /// <returns>The modified or newly created Revit Space object.</returns>
  private DB.Space CreateRevitSpaceIfNeeded(
    Space speckleSpace,
    DB.Space revitSpace,
    Phase targetPhase,
    Level level,
    XYZ basePoint
  )
  {
    // Main logic
    if (revitSpace == null)
    {
      revitSpace = CreateNewSpace(level, targetPhase, new UV(basePoint.X, basePoint.Y), speckleSpace.level.name);

      if (revitSpace == null)
      {
        return null;
      }
    }
    else if (revitSpace.Area == 0 && revitSpace.Location == null)
    {
      Doc.Delete(revitSpace.Id);

      revitSpace = CreateNewSpace(level, targetPhase, new UV(basePoint.X, basePoint.Y), speckleSpace.level.name);
    }

    return revitSpace;
  }

  /// <summary>
  /// Creates a new RevitSpace based on the provided parameters.
  /// </summary>
  /// <param name="level">The level for the new space.</param>
  /// <param name="targetPhase">The target phase for the new space. If null, the active view phase will be used.</param>
  /// <param name="basePoint">The base point (UV coordinates) for the new space.</param>
  /// <param name="levelName">The name of the level. Used to determine if the space has a location.</param>
  /// <returns>The newly created RevitSpace, or null if the space could not be created.</returns>
  private DB.Space CreateNewSpace(Level level, Phase targetPhase, UV basePoint, string levelName)
  {
    if (targetPhase == null)
    {
      return string.IsNullOrEmpty(levelName) || basePoint == null
        ? null
        : Doc.Create.NewSpace(level, new UV(basePoint.U, basePoint.V));
    }

    return string.IsNullOrEmpty(levelName) // has no location
      ? Doc.Create.NewSpace(targetPhase)
      : Doc.Create.NewSpace(level, targetPhase, new UV(basePoint.U, basePoint.V));
  }

  /// <summary>
  /// Determines the target phase for a space based on available information.
  /// </summary>
  /// <param name="speckleSpace">The Space object from the Speckle system.</param>
  /// <param name="revitSpace">The Space object from the Revit system.</param>
  /// <returns>The determined target Phase object.</returns>
  /// <remarks>
  /// The method tries to determine the target phase based on the following priority:
  /// 1. Phase from the Speckle space (if it exists).
  /// 2. Phase from the existing Revit space (if it exists).
  /// 3. Phase from the active view in Revit.
  /// </remarks>
  private Phase DetermineTargetPhase(Space speckleSpace, DB.Space revitSpace)
  {
    // Get all phases
    var phases = Doc.Phases.Cast<Phase>().ToList();

    // Determine existing space phase, if any
    Phase existingSpacePhase = null;
    if (revitSpace != null)
    {
      string existingSpacePhaseName = revitSpace.get_Parameter(BuiltInParameter.ROOM_PHASE).AsValueString();
      existingSpacePhase = phases.FirstOrDefault(x => x.Name == existingSpacePhaseName);
    }

    // Determine target phase
    // Priority: speckleSpace phase > existing space phase > active view phase
    string targetPhaseName = speckleSpace.phaseName;
    var targetPhase = phases.FirstOrDefault(x => x.Name == targetPhaseName) ?? existingSpacePhase;

    return targetPhase;
  }

  private Phase DetermineActiveViewPhase(IEnumerable<Phase> phases = null)
  {
    phases ??= Doc.Phases.Cast<Phase>();

    // Determine active view phase
    var activeViewPhaseName = Doc.ActiveView.get_Parameter(BuiltInParameter.VIEW_PHASE).AsValueString();
    return phases.FirstOrDefault(x => x.Name == activeViewPhaseName);
  }

  public Space SpaceToSpeckle(DB.Space revitSpace)
  {
    var profiles = GetProfiles(revitSpace);

    var speckleSpace = new Space
    {
      name = revitSpace.Name,
      number = revitSpace.Number,
      basePoint = (Point)LocationToSpeckle(revitSpace),
      level = ConvertAndCacheLevel(revitSpace.LevelId, revitSpace.Document),
      topLevel = ConvertAndCacheLevel(
        revitSpace.get_Parameter(BuiltInParameter.ROOM_UPPER_LEVEL).AsElementId(),
        revitSpace.Document
      ),
      baseOffset = GetParamValue<double>(revitSpace, BuiltInParameter.ROOM_LOWER_OFFSET),
      topOffset = GetParamValue<double>(revitSpace, BuiltInParameter.ROOM_UPPER_OFFSET),
      outline = profiles.Count != 0 ? profiles[0] : null,
      area = GetParamValue<double>(revitSpace, BuiltInParameter.ROOM_AREA),
      volume = GetParamValue<double>(revitSpace, BuiltInParameter.ROOM_VOLUME),
      spaceType = revitSpace.SpaceType.ToString(),
      displayValue = GetElementDisplayValue(revitSpace)
    };

    if (profiles.Count > 1)
    {
      speckleSpace.voids = profiles.Skip(1).ToList();
    }

    // Spaces are typically associated with a Room, but not always
    if (revitSpace.Room != null)
    {
      speckleSpace["roomId"] = revitSpace.Room.Id.ToString();
    }

    // Zones are stored as a Space prop despite being a parent object, so we need to convert it here
    speckleSpace.zone = revitDocumentAggregateCache
      .GetOrInitializeEmptyCacheOfType<RevitZone>(out _)
      .GetOrAdd(revitSpace.Zone.Name, () => ZoneToSpeckle(revitSpace.Zone), out _);

    GetAllRevitParamsAndIds(speckleSpace, revitSpace);

    // Special Phase handling for Spaces, phase is found as a parameter on the Space object, not a property
    var phase = Doc.GetElement(revitSpace.get_Parameter(BuiltInParameter.ROOM_PHASE).AsElementId());
    if (phase != null)
    {
      speckleSpace.phaseName = phase.Name;
    }

    return speckleSpace;
  }
}
