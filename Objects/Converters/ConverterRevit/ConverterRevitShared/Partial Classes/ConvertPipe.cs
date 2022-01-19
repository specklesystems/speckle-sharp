using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using Arc = Objects.Geometry.Arc;
using Curve = Objects.Geometry.Curve;
using Line = Objects.Geometry.Line;
using Polyline = Objects.Geometry.Polyline;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public List<ApplicationPlaceholderObject> PipeToNative(BuiltElements.Pipe specklePipe)
    {
      var speckleRevitPipe = specklePipe as RevitPipe;

      // check to see if pipe already exists in the doc
      var isUpdate = false;
      var docObj = GetExistingElementByApplicationId(specklePipe.applicationId);

      Level level = null;
      Element element = null;
      switch (specklePipe.baseCurve)
      {
        case Line line:
          var _line = LineToNative(line);
          var linePipeType = GetElementType<DB.Plumbing.PipeType>(specklePipe);
          level = LevelToNative(speckleRevitPipe != null ? speckleRevitPipe.level : LevelFromCurve(_line));
          DB.Plumbing.Pipe linePipe = CreateLinearPipe(_line, speckleRevitPipe, level, linePipeType);
          if (docObj != null)
          {
            var lineSystem = linePipe.MEPSystem.Id;
            linePipe = (DB.Plumbing.Pipe)docObj;
            linePipe.SetSystemType(lineSystem);
            ((LocationCurve)linePipe.Location).Curve = _line;
            isUpdate = true;
          }
          element = linePipe;
          break;
        case Arc arc:
          var _arc = ArcToNative(arc);
          var arcPipeType = GetElementType<DB.Plumbing.FlexPipeType>(specklePipe);
          level = LevelToNative(speckleRevitPipe != null ? speckleRevitPipe.level : LevelFromCurve(_arc));
          DB.Plumbing.FlexPipe arcPipe = CreateFlexPipe(_arc, speckleRevitPipe, level, arcPipeType);
          if (docObj != null)
          {
            var arcSystem = arcPipe.MEPSystem.Id;
            linePipe = (DB.Plumbing.Pipe)docObj;
            linePipe.SetSystemType(arcSystem);
            ((LocationCurve)linePipe.Location).Curve = _arc;
            isUpdate = true;
          }
          element = arcPipe;
          break;
        case Polyline polyline:
          //level = LevelToNative(speckleRevitPipe != null ? speckleRevitPipe.level : LevelFromPoint(revitCurve));
          // this requires special handling to only extract vertices
          break;
        default:
          Report.LogConversionError(new Exception($"Pipe baseCurve type of ${specklePipe.speckle_type} cannot be used to create pipes."));
          return null;
      }

      if (speckleRevitPipe != null)
      {
        TrySetParam(element, BuiltInParameter.RBS_START_LEVEL_PARAM, level);
        SetInstanceParameters(element, speckleRevitPipe);
      }
      TrySetParam(element, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM, specklePipe.diameter, specklePipe.units);

      var placeholders = new List<ApplicationPlaceholderObject>
      {
        new ApplicationPlaceholderObject
          {applicationId = specklePipe.applicationId, ApplicationGeneratedId = element.UniqueId, NativeObject = element}
      };
      Report.Log($"{(isUpdate ? "Updated" : "Created")} Pipe {element.Id}");
      return placeholders;
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
        systemName = revitPipe.MEPSystem.Name,
        systemType = GetParamValue<string>(revitPipe, BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM),
        diameter = GetParamValue<double>(revitPipe, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM),
        length = GetParamValue<double>(revitPipe, BuiltInParameter.CURVE_ELEM_LENGTH),
        level = ConvertAndCacheLevel(revitPipe, BuiltInParameter.RBS_START_LEVEL_PARAM),
        displayMesh = GetElementMesh(revitPipe)
      };

      GetAllRevitParamsAndIds(specklePipe, revitPipe, new List<string>
      {
        "RBS_SYSTEM_CLASSIFICATION_PARAM",
        "RBS_PIPING_SYSTEM_TYPE_PARAM",
        "RBS_SYSTEM_NAME_PARAM",
        "RBS_PIPE_DIAMETER_PARAM",
        "CURVE_ELEM_LENGTH",
        "RBS_START_LEVEL_PARAM",
      });
      //Report.Log($"Converted Pipe {revitPipe.Id}");
      return specklePipe;
    }
    public BuiltElements.Pipe PipeToSpeckle(DB.Plumbing.FlexPipe revitPipe)
    {
      // geometry 
      var baseGeometry = LocationToSpeckle(revitPipe);
      if (!(baseGeometry is Curve baseCurve))
      {
        throw new Speckle.Core.Logging.SpeckleException("Could not register pipe base geometry as a Curve");
      }

      // speckle pipe
      var specklePipe = new RevitPipe
      {
        baseCurve = baseCurve,
        family = revitPipe.FlexPipeType.FamilyName,
        type = revitPipe.FlexPipeType.Name,
        systemName = revitPipe.MEPSystem.Name,
        systemType = GetParamValue<string>(revitPipe, BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM),
        diameter = GetParamValue<double>(revitPipe, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM),
        length = GetParamValue<double>(revitPipe, BuiltInParameter.CURVE_ELEM_LENGTH),
        level = ConvertAndCacheLevel(revitPipe, BuiltInParameter.RBS_START_LEVEL_PARAM),
        displayMesh = GetElementMesh(revitPipe)
      };

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

    private DB.Plumbing.Pipe CreateLinearPipe(DB.Line baseLine, RevitPipe speckleRevitPipe, Level level, DB.Plumbing.PipeType type)
    {
      DB.Plumbing.Pipe pipe = null;

      // get MEP pipe system by name or by type
      var types = new FilteredElementCollector(Doc).WhereElementIsElementType()
        .OfClass(typeof(DB.Plumbing.PipingSystemType)).ToElements().Cast<ElementType>().ToList();
      var systemFamily = speckleRevitPipe?.systemType ?? "";
      var system = types.FirstOrDefault(x => x.Name == speckleRevitPipe?.systemName) ??
                   types.FirstOrDefault(x => x.Name == systemFamily);
      if (system == null)
      {
        system = types.FirstOrDefault();
        Report.LogConversionError(new Exception($"Pipe type {systemFamily} not found; replaced with {system.Name}"));
      }

      // create the pipe
      pipe = DB.Plumbing.Pipe.Create(Doc, system.Id, type.Id, level.Id, baseLine.GetEndPoint(0), baseLine.GetEndPoint(1));

      return pipe;
    }
    private DB.Plumbing.FlexPipe CreateFlexPipe(DB.Curve baseCurve, RevitPipe speckleRevitPipe, Level level, DB.Plumbing.FlexPipeType type)
    {
      DB.Plumbing.FlexPipe pipe = null;

      // get MEP pipe system by name or by type
      var types = new FilteredElementCollector(Doc).WhereElementIsElementType()
        .OfClass(typeof(DB.Plumbing.PipingSystemType)).ToElements().Cast<ElementType>().ToList();
      var systemFamily = speckleRevitPipe?.systemType ?? "";
      var system = types.FirstOrDefault(x => x.Name == speckleRevitPipe?.systemName) ??
                   types.FirstOrDefault(x => x.Name == systemFamily);
      if (system == null)
      {
        system = types.FirstOrDefault();
        Report.LogConversionError(new Exception($"Pipe type {systemFamily} not found; replaced with {system.Name}"));
      }

      // create the pipe
      switch (baseCurve)
      {
        case DB.Arc arc:
          var st = new XYZ();
          var et = new XYZ();
          var arcPoints = new List<XYZ>() { baseCurve.GetEndPoint(0), baseCurve.GetEndPoint(1) };
          pipe = DB.Plumbing.FlexPipe.Create(Doc, system.Id, type.Id, level.Id, st, et, arcPoints);
          break;
        default:
          break;
      }

      return pipe;
    }
  }
}