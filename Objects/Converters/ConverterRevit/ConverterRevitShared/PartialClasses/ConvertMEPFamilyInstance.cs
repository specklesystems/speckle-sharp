using DB = Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Autodesk.Revit.DB;
using System.Linq;
using System;
using System.Collections.Generic;
using Speckle.Core.Models;
using Speckle.Core.Kits;
using RevitSharedResources.Models;
using Speckle.Core.Logging;
using RevitSharedResources.Extensions.SpeckleExtensions;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  public List<PartType> FittingPartTypes { get; } =
    new List<PartType>() { PartType.Elbow, PartType.Tee, PartType.Cross, PartType.Transition, PartType.Union };

  public RevitMEPFamilyInstance MEPFamilyInstanceToSpeckle(
    DB.FamilyInstance familyInstance,
    RevitMEPFamilyInstance existingSpeckleObject = null
  )
  {
    var speckleFi = existingSpeckleObject ?? new RevitMEPFamilyInstance();

    var partType = GetParamValue<PartType>(familyInstance.Symbol.Family, BuiltInParameter.FAMILY_CONTENT_PART_TYPE);
    speckleFi.RevitPartType = partType.ToString();

    foreach (var connector in familyInstance.MEPModel?.ConnectorManager?.Connectors?.Cast<Connector>())
    {
      speckleFi.Connectors.Add(ConnectorToSpeckle(connector));
    }

    using var coarseOptions = new Options() { DetailLevel = ViewDetailLevel.Coarse };
    foreach (var curve in GetCurvesFromGeom(familyInstance.get_Geometry(coarseOptions)))
    {
      speckleFi.Curves.Add(CurveToSpeckle(curve, familyInstance.Document));
    }

    _ = RevitInstanceToSpeckle(familyInstance, out _, null, existingInstance: speckleFi);
    return speckleFi;
  }

  private IEnumerable<DB.Curve> GetCurvesFromGeom(GeometryElement geometryElement)
  {
    foreach (var geomObj in geometryElement)
    {
      switch (geomObj)
      {
        case DB.Curve curve:
          yield return curve;
          break;
        case DB.GeometryInstance i:
          foreach (var c in GetCurvesFromGeom(i.GetInstanceGeometry()))
          {
            yield return c;
          }
          break;
      }
    }
  }

  public DB.FamilyInstance MEPFamilyInstanceToNative(RevitMEPFamilyInstance speckleFi, ApplicationObject appObj)
  {
    _ = RevitInstanceToNative(speckleFi, appObj);
    var revitFi = (DB.FamilyInstance)appObj.Converted.First();

    CreateSystemConnections(speckleFi.Connectors, revitFi, receivedObjectsCache);

    return revitFi;
  }

  public ApplicationObject FittingOrMEPInstanceToNative(RevitMEPFamilyInstance speckleFi)
  {
    var appObj = new ApplicationObject(speckleFi.id, speckleFi.speckle_type)
    {
      applicationId = speckleFi.applicationId
    };

    if (Enum.TryParse<PartType>(speckleFi.RevitPartType, out var partType) && FittingPartTypes.Contains(partType))
    {
      try
      {
        _ = FittingToNative(speckleFi, partType, appObj);
        return appObj;
      }
      catch (ConversionNotReadyException)
      {
        var notReadyData = revitDocumentAggregateCache
          .TryGetCacheOfType<ConversionNotReadyCacheData>()
          ?.TryGet(speckleFi.id);

        if (notReadyData == null || !notReadyData.HasValue || notReadyData.Value.NumberOfTimesCaught < 2)
        {
          throw;
        }
        else
        {
          SpeckleLog.Logger.Error(
            "Could not create fitting as part of the system. Reason: Speckle object of type RevitMEPFamilyInstance was waiting for an object to convert that never did. Converting as independent instance instead"
          );
          appObj.Update(
            logItem: $"Could not create fitting as part of the system. Reason: Speckle object of type {speckleFi.GetType()} was waiting for an object to convert that never did. Converting as independent instance instead"
          );
          _ = MEPFamilyInstanceToNative(speckleFi, appObj);
          return appObj;
        }
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        SpeckleLog.Logger.LogDefaultError(ex);
        appObj.Update(
          logItem: $"Could not create fitting as part of the system. Reason: {ex.Message}. Converting as independent instance instead"
        );
        _ = MEPFamilyInstanceToNative(speckleFi, appObj);
        return appObj;
      }
    }
    else
    {
      _ = MEPFamilyInstanceToNative(speckleFi, appObj);
      return appObj;
    }
  }
}
