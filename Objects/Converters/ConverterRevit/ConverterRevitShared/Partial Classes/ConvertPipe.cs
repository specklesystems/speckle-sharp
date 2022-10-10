﻿using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Curve = Objects.Geometry.Curve;
using DB = Autodesk.Revit.DB;
using Line = Objects.Geometry.Line;
using Polyline = Objects.Geometry.Polyline;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject PipeToNative(BuiltElements.Pipe specklePipe)
    {
      var speckleRevitPipe = specklePipe as RevitPipe;

      // check to see if pipe already exists in the doc
      var docObj = GetExistingElementByApplicationId(specklePipe.applicationId);
      var appObj = new ApplicationObject(specklePipe.id, specklePipe.speckle_type) { applicationId = specklePipe.applicationId };

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(docObj, appObj, out appObj))
        return appObj;

      // get system info
      if (!GetElementType<DB.Plumbing.PipeType>(specklePipe, appObj, out DB.Plumbing.PipeType pipeType))
      {
        appObj.Update(status: ApplicationObject.State.Failed);
        return appObj;
      }
      var systemTypes = new FilteredElementCollector(Doc).WhereElementIsElementType()
       .OfClass(typeof(DB.Plumbing.PipingSystemType)).ToElements().Cast<ElementType>().ToList();
      var systemFamily = speckleRevitPipe?.systemType ?? "";
      var system = systemTypes.FirstOrDefault(x => x.Name == speckleRevitPipe?.systemName) ??
                   systemTypes.FirstOrDefault(x => x.Name == systemFamily);
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
          DB.Level level = ConvertLevelToRevit(speckleRevitPipe != null ? speckleRevitPipe.level : LevelFromCurve(baseLine), out levelState);
          var linePipe = DB.Plumbing.Pipe.Create(Doc, system.Id, pipeType.Id, level.Id, baseLine.GetEndPoint(0), baseLine.GetEndPoint(1));
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
            if (!GetElementType<DB.Plumbing.FlexPipeType>(speckleRevitFlexPipe, appObj, out flexPipeType))
            {
              appObj.Update(status: ApplicationObject.State.Failed);
              return appObj;
            }
          }
          else
          {
            if (!GetElementType<DB.Plumbing.FlexPipeType>(specklePipe, appObj, out flexPipeType))
            {
              appObj.Update(status: ApplicationObject.State.Failed);
              return appObj;
            }
          }

          // get points
          Polyline basePoly = specklePipe.baseCurve as Polyline;
          if (specklePipe.baseCurve is Curve curve)
          {
            basePoly = curve.displayValue;
            var baseCurve = CurveToNative(curve);
            var start = baseCurve.GetEndPoint(0);
            var end = baseCurve.GetEndPoint(1);
          }
          if (basePoly == null) break;
          var polyPoints = basePoly.GetPoints().Select(o => PointToNative(o)).ToList();

          // get tangents if they exist
          XYZ startTangent = (speckleRevitFlexPipe != null) ? VectorToNative(speckleRevitFlexPipe.startTangent) : null;
          XYZ endTangent = (speckleRevitFlexPipe != null) ? VectorToNative(speckleRevitFlexPipe.endTangent) : null;

          // get level
          DB.Level flexPolyLevel = ConvertLevelToRevit(speckleRevitFlexPipe != null ? speckleRevitFlexPipe.level : LevelFromPoint(polyPoints.First()), out levelState);

          var flexPolyPipe = (startTangent != null && endTangent != null) ?
            DB.Plumbing.FlexPipe.Create(Doc, system.Id, flexPipeType.Id, flexPolyLevel.Id, startTangent, endTangent, polyPoints) :
            DB.Plumbing.FlexPipe.Create(Doc, system.Id, flexPipeType.Id, flexPolyLevel.Id, polyPoints);

          if (docObj != null)
            Doc.Delete(docObj.Id); // deleting instead of updating for now!

          pipe = flexPolyPipe;
          break;
        default:
          appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Curve of type {specklePipe.baseCurve.GetType()} cannot be used to create a Revit Pipe");
          return appObj;
      }

      if (speckleRevitPipe != null)
        SetInstanceParameters(pipe, speckleRevitPipe);

      TrySetParam(pipe, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM, specklePipe.diameter, specklePipe.units);

      appObj.Update(status: ApplicationObject.State.Created, createdId: pipe.UniqueId, convertedItem: pipe);
      return appObj;
    }

    public BuiltElements.Pipe PipeToSpeckle(DB.Plumbing.Pipe revitPipe)
    {
      // geometry 
      var baseGeometry = LocationToSpeckle(revitPipe);
      if (!(baseGeometry is Line baseLine))
        throw new Speckle.Core.Logging.SpeckleException("Only line based Pipes are currently supported.");

      // speckle pipe
      var specklePipe = new RevitPipe
      {
        baseCurve = baseLine,
        family = revitPipe.PipeType.FamilyName,
        type = revitPipe.PipeType.Name,
        systemName = revitPipe.MEPSystem.Name,
        systemType = GetParamValue<string>(revitPipe, BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM),
        diameter = GetParamValue<double>(revitPipe, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM),
        length = GetParamValue<double>(revitPipe, BuiltInParameter.CURVE_ELEM_LENGTH),
        level = ConvertAndCacheLevel(revitPipe, BuiltInParameter.RBS_START_LEVEL_PARAM),
        displayValue = GetElementMesh(revitPipe)
      };

      var material = ConverterRevit.GetMEPSystemMaterial(revitPipe);
      if (material != null)
        foreach (var mesh in specklePipe.displayValue)
          mesh["renderMaterial"] = material;

      GetAllRevitParamsAndIds(specklePipe, revitPipe, new List<string>
      {
        "RBS_PIPING_SYSTEM_TYPE_PARAM",
        "RBS_SYSTEM_CLASSIFICATION_PARAM",
        "RBS_SYSTEM_NAME_PARAM",
        "RBS_PIPE_DIAMETER_PARAM",
        "CURVE_ELEM_LENGTH",
        "RBS_START_LEVEL_PARAM",
      });
      return specklePipe;
    }
    public BuiltElements.Pipe PipeToSpeckle(DB.Plumbing.FlexPipe revitPipe)
    {
      // create polyline from revitpipe points
      var polyline = new Polyline();
      polyline.value = PointsToFlatList(revitPipe.Points.Select(o => PointToSpeckle(o)));
      polyline.units = ModelUnits;
      polyline.closed = false;

      // speckle pipe
      var specklePipe = new RevitFlexPipe
      {
        baseCurve = polyline,
        family = revitPipe.FlexPipeType.FamilyName,
        type = revitPipe.FlexPipeType.Name,
        systemName = revitPipe.MEPSystem.Name,
        systemType = GetParamValue<string>(revitPipe, BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM),
        diameter = GetParamValue<double>(revitPipe, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM),
        length = GetParamValue<double>(revitPipe, BuiltInParameter.CURVE_ELEM_LENGTH),
        startTangent = VectorToSpeckle(revitPipe.StartTangent),
        endTangent = VectorToSpeckle(revitPipe.EndTangent),
        level = ConvertAndCacheLevel(revitPipe, BuiltInParameter.RBS_START_LEVEL_PARAM),
        displayValue = GetElementMesh(revitPipe)
      };

      var material = ConverterRevit.GetMEPSystemMaterial(revitPipe);

      if (material != null)
        foreach (var mesh in specklePipe.displayValue)
          mesh["renderMaterial"] = material;

      GetAllRevitParamsAndIds(specklePipe, revitPipe, new List<string>
      {
        "RBS_SYSTEM_CLASSIFICATION_PARAM",
        "RBS_PIPING_SYSTEM_TYPE_PARAM",
        "RBS_SYSTEM_NAME_PARAM",
        "RBS_PIPE_DIAMETER_PARAM",
        "CURVE_ELEM_LENGTH",
        "RBS_START_LEVEL_PARAM",
      });

      return specklePipe;
    }
  }
}