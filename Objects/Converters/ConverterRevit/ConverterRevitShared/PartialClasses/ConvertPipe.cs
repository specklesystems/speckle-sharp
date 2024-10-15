using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using ConverterRevitShared.Extensions;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using Curve = Objects.Geometry.Curve;
using DB = Autodesk.Revit.DB;
using Line = Objects.Geometry.Line;
using Polyline = Objects.Geometry.Polyline;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  public ApplicationObject PipeToNative(BuiltElements.Pipe specklePipe)
  {
    var speckleRevitPipe = specklePipe as RevitPipe;

    // check to see if pipe already exists in the doc
    Element docObj = GetExistingElementByApplicationId(specklePipe.applicationId);
    var appObj = new ApplicationObject(specklePipe.id, specklePipe.speckle_type)
    {
      applicationId = specklePipe.applicationId
    };

    // skip if element already exists in doc & receive mode is set to ignore
    if (IsIgnore(docObj, appObj))
    {
      return appObj;
    }

    // get system info
    MEPCurveType pipeType = GetElementType<DB.MEPCurveType>(specklePipe, appObj, out bool _);
    if (pipeType == null)
    {
      appObj.Update(status: ApplicationObject.State.Failed);
      return appObj;
    }

    List<ElementType> systemTypes = new FilteredElementCollector(Doc)
      .WhereElementIsElementType()
      .OfClass(typeof(DB.Plumbing.PipingSystemType))
      .ToElements()
      .Cast<ElementType>()
      .ToList();
    var systemFamily = speckleRevitPipe?.systemType ?? "";
    ElementType system =
      systemTypes.FirstOrDefault(x => x.Name == speckleRevitPipe?.systemName)
      ?? systemTypes.FirstOrDefault(x => x.Name == systemFamily);
    if (system == null)
    {
      system = systemTypes.FirstOrDefault();
      appObj.Update(logItem: $"Pipe type {systemFamily} not found; replaced with {system.Name}");
    }

    Element pipe = null;
    var levelState = ApplicationObject.State.Unknown;
    switch (specklePipe.baseCurve)
    {
      case Line line:
        DB.Line baseLine = LineToNative(line);
        DB.Level level = ConvertLevelToRevit(
          speckleRevitPipe != null ? speckleRevitPipe.level : LevelFromCurve(baseLine),
          out levelState
        );
        var linePipe = DB.Plumbing.Pipe.Create(
          Doc,
          system.Id,
          pipeType.Id,
          level.Id,
          baseLine.GetEndPoint(0),
          baseLine.GetEndPoint(1)
        );
        if (docObj != null)
        {
          var lineSystem = linePipe.MEPSystem.Id;
          linePipe = (DB.Plumbing.Pipe)docObj;
          linePipe.SetSystemType(lineSystem);
          ((LocationCurve)linePipe.Location).Curve = baseLine;
        }

        pipe = linePipe;
        break;
      case Polyline _:
      case Curve _:
        var speckleRevitFlexPipe = specklePipe as RevitFlexPipe;
        DB.Plumbing.FlexPipeType flexPipeType = null;
        if (speckleRevitFlexPipe != null)
        {
          flexPipeType = GetElementType<FlexPipeType>(speckleRevitFlexPipe, appObj, out bool _);
        }
        else
        {
          flexPipeType = GetElementType<FlexPipeType>(specklePipe, appObj, out bool _);
        }

        if (flexPipeType == null)
        {
          appObj.Update(status: ApplicationObject.State.Failed);
          return appObj;
        }

        // get points
        Polyline basePoly = specklePipe.baseCurve as Polyline;
        if (specklePipe.baseCurve is Curve curve)
        {
          basePoly = curve.displayValue;
          DB.Curve baseCurve = CurveToNative(curve);
          XYZ start = baseCurve.GetEndPoint(0);
          XYZ end = baseCurve.GetEndPoint(1);
        }

        if (basePoly == null)
        {
          break;
        }

        var polyPoints = basePoly.GetPoints().Select(o => PointToNative(o)).ToList();

        // get tangents if they exist
        XYZ startTangent = (speckleRevitFlexPipe != null) ? VectorToNative(speckleRevitFlexPipe.startTangent) : null;
        XYZ endTangent = (speckleRevitFlexPipe != null) ? VectorToNative(speckleRevitFlexPipe.endTangent) : null;

        // get level
        DB.Level flexPolyLevel = ConvertLevelToRevit(
          speckleRevitFlexPipe != null ? speckleRevitFlexPipe.level : LevelFromPoint(polyPoints.First()),
          out levelState
        );

        FlexPipe flexPolyPipe =
          (startTangent != null && endTangent != null)
            ? DB.Plumbing.FlexPipe.Create(
              Doc,
              system.Id,
              flexPipeType.Id,
              flexPolyLevel.Id,
              startTangent,
              endTangent,
              polyPoints
            )
            : DB.Plumbing.FlexPipe.Create(Doc, system.Id, flexPipeType.Id, flexPolyLevel.Id, polyPoints);

        // deleting instead of updating for now!
        if (docObj != null)
        {
          Doc.Delete(docObj.Id);
        }

        pipe = flexPolyPipe;
        break;
      default:
        appObj.Update(
          status: ApplicationObject.State.Failed,
          logItem: $"Curve of type {specklePipe.baseCurve.GetType()} cannot be used to create a Revit Pipe"
        );
        return appObj;
    }

    if (speckleRevitPipe != null)
    {
      SetInstanceParameters(pipe, speckleRevitPipe);
      CreateSystemConnections(speckleRevitPipe.Connectors, pipe, receivedObjectsCache);
    }

    TrySetParam(pipe, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM, specklePipe.diameter, specklePipe.units);

    appObj.Update(status: ApplicationObject.State.Created, createdId: pipe.UniqueId, convertedItem: pipe);
    return appObj;
  }

  public BuiltElements.Pipe PipeToSpeckle(DB.Plumbing.Pipe revitPipe)
  {
    // geometry
    var baseGeometry = LocationToSpeckle(revitPipe);
    if (!(baseGeometry is Line baseLine))
    {
      throw new Speckle.Core.Logging.SpeckleException("Only line based Pipes are currently supported.");
    }

    // speckle pipe
    var specklePipe = new RevitPipe
    {
      baseCurve = baseLine,
      family = revitPipe.PipeType.FamilyName,
      type = revitPipe.PipeType.Name,
      systemName = revitPipe.MEPSystem?.Name ?? "",
      systemType = GetParamValue<string>(revitPipe, BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM),
      diameter = GetParamValue<double>(revitPipe, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM),
      length = GetParamValue<double>(revitPipe, BuiltInParameter.CURVE_ELEM_LENGTH),
      level = ConvertAndCacheLevel(revitPipe, BuiltInParameter.RBS_START_LEVEL_PARAM),
      displayValue = GetElementDisplayValue(revitPipe)
    };

    var material = ConverterRevit.GetMEPSystemMaterial(revitPipe);
    if (material != null)
    {
      foreach (var mesh in specklePipe.displayValue)
      {
        mesh["renderMaterial"] = material;
      }
    }

    GetAllRevitParamsAndIds(
      specklePipe,
      revitPipe,
      new List<string>
      {
        "RBS_PIPING_SYSTEM_TYPE_PARAM",
        "RBS_SYSTEM_CLASSIFICATION_PARAM",
        "RBS_SYSTEM_NAME_PARAM",
        "RBS_PIPE_DIAMETER_PARAM",
        "CURVE_ELEM_LENGTH",
        "RBS_START_LEVEL_PARAM",
        "RBS_CURVE_HOR_OFFSET_PARAM",
        "RBS_CURVE_VERT_OFFSET_PARAM",
        "RBS_PIPE_BOTTOM_ELEVATION",
        "RBS_PIPE_TOP_ELEVATION"
      }
    );

    foreach (var connector in revitPipe.GetConnectorSet())
    {
      specklePipe.Connectors.Add(ConnectorToSpeckle(connector));
    }

    return specklePipe;
  }

  public BuiltElements.Pipe PipeToSpeckle(DB.Plumbing.FlexPipe revitPipe)
  {
    // create polyline from revitpipe points
    var polyline = new Polyline();
    polyline.value = PointsToFlatList(revitPipe.Points.Select(o => PointToSpeckle(o, revitPipe.Document)));
    polyline.units = ModelUnits;
    polyline.closed = false;

    // speckle pipe
    var specklePipe = new RevitFlexPipe
    {
      baseCurve = polyline,
      family = revitPipe.FlexPipeType.FamilyName,
      type = revitPipe.FlexPipeType.Name,
      systemName = revitPipe.MEPSystem?.Name ?? "",
      systemType = GetParamValue<string>(revitPipe, BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM),
      diameter = GetParamValue<double>(revitPipe, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM),
      length = GetParamValue<double>(revitPipe, BuiltInParameter.CURVE_ELEM_LENGTH),
      startTangent = VectorToSpeckle(revitPipe.StartTangent, revitPipe.Document),
      endTangent = VectorToSpeckle(revitPipe.EndTangent, revitPipe.Document),
      level = ConvertAndCacheLevel(revitPipe, BuiltInParameter.RBS_START_LEVEL_PARAM),
      displayValue = GetElementDisplayValue(revitPipe)
    };

    var material = ConverterRevit.GetMEPSystemMaterial(revitPipe);

    if (material != null)
    {
      foreach (var mesh in specklePipe.displayValue)
      {
        mesh["renderMaterial"] = material;
      }
    }

    GetAllRevitParamsAndIds(
      specklePipe,
      revitPipe,
      new List<string>
      {
        "RBS_SYSTEM_CLASSIFICATION_PARAM",
        "RBS_PIPING_SYSTEM_TYPE_PARAM",
        "RBS_SYSTEM_NAME_PARAM",
        "RBS_PIPE_DIAMETER_PARAM",
        "CURVE_ELEM_LENGTH",
        "RBS_START_LEVEL_PARAM",
      }
    );

    foreach (var connector in revitPipe.GetConnectorSet())
    {
      specklePipe.Connectors.Add(ConnectorToSpeckle(connector));
    }

    return specklePipe;
  }
}
