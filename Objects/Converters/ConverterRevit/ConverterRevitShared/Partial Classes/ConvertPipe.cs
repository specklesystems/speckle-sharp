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

      // check to see if pipe already exists in the doc
      var docObj = GetExistingElementByApplicationId(specklePipe.applicationId);

      Element pipe = null;
      if (specklePipe.baseCurve is Line line)
      {
        var pipeType = GetElementType<DB.Plumbing.PipeType>(specklePipe);

        DB.Line baseLine = LineToNative(line);
        DB.Level level = LevelToNative(speckleRevitPipe != null ? speckleRevitPipe.level : LevelFromCurve(baseLine));
        var linePipe = DB.Plumbing.Pipe.Create(Doc, system.Id, pipeType.Id, level.Id, baseLine.GetEndPoint(0), baseLine.GetEndPoint(1));
        if (docObj != null)
        {
          var lineSystem = linePipe.MEPSystem.Id;
          linePipe = (DB.Plumbing.Pipe)docObj;
          linePipe.SetSystemType(lineSystem);
          ((LocationCurve)linePipe.Location).Curve = baseLine;
        }
        pipe = linePipe;
      }
      else if (specklePipe.baseCurve is Polyline polyline)
      {
        var speckleRevitFlexPipe = specklePipe as RevitFlexPipe;
        var pipeType = GetElementType<DB.Plumbing.FlexPipeType>(speckleRevitFlexPipe);

        var points = polyline.GetPoints().Select(o => PointToNative(o)).ToList();
        DB.Level level = LevelToNative(speckleRevitFlexPipe != null ? speckleRevitFlexPipe.level : LevelFromPoint(points.First()));
        var startTangent = VectorToNative(speckleRevitFlexPipe.startTangent);
        var endTangent = VectorToNative(speckleRevitFlexPipe.endTangent);
        var flexPipe = DB.Plumbing.FlexPipe.Create(Doc, system.Id, pipeType.Id, level.Id, startTangent, endTangent, points);
        
        if (docObj != null)
          Doc.Delete(docObj.Id); // deleting instead of updating for now!

        pipe = flexPipe;
      }
      else
      {
        Report.LogConversionError(new Exception($"Pipe BaseCurve of type ${specklePipe.baseCurve.GetType()} cannot be used to create a Revit Pipe"));
      }

      if (speckleRevitPipe != null)
      {
        SetInstanceParameters(pipe, speckleRevitPipe);
      }
      TrySetParam(pipe, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM, specklePipe.diameter, specklePipe.units);

      var placeholders = new List<ApplicationPlaceholderObject>
      {
        new ApplicationPlaceholderObject
          {applicationId = specklePipe.applicationId, ApplicationGeneratedId = pipe.UniqueId, NativeObject = pipe}
      };

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
        "RBS_PIPING_SYSTEM_TYPE_PARAM",
        "RBS_SYSTEM_CLASSIFICATION_PARAM",
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
  }
}