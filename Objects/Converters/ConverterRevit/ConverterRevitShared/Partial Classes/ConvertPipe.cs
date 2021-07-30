using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using Line = Objects.Geometry.Line;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public List<ApplicationPlaceholderObject> PipeToNative(BuiltElements.Pipe specklePipe)
    {
      // check if this is a line based pipe, return null if not
      DB.Line baseLine = null;
      switch (specklePipe.baseCurve)
      {
        case Line line:
          baseLine = LineToNative(line);
          break;
        default:
          ConversionErrors.Add(new Exception($"Pipe baseCurve is not a line"));
          return null;
      }

      // geometry
      var speckleRevitPipe = specklePipe as RevitPipe;
      var level = LevelToNative(speckleRevitPipe != null ? speckleRevitPipe.level : LevelFromCurve(baseLine));

      // get MEP pipe system by name or by type
      var pipeType = GetElementType<DB.Plumbing.PipeType>(specklePipe);
      var types = new FilteredElementCollector(Doc).WhereElementIsElementType()
        .OfClass(typeof(DB.Plumbing.PipingSystemType)).ToElements().Cast<ElementType>().ToList();
      var systemFamily = speckleRevitPipe?.systemType ?? "";
      var system = types.FirstOrDefault(x => x.Name == speckleRevitPipe?.systemName) ??
                   types.FirstOrDefault(x => x.Name == systemFamily);
      if ( system == null )
      {
        system = types.FirstOrDefault();
        ConversionErrors.Add(new Exception($"Pipe type {systemFamily} not found; replaced with {system.Name}"));
      }

      // create or update the pipe
      DB.Plumbing.Pipe pipe;
      var docObj = GetExistingElementByApplicationId(specklePipe.applicationId);
      if ( docObj == null )
      {
        pipe = DB.Plumbing.Pipe.Create(Doc, system.Id, pipeType.Id, level.Id,
          baseLine.GetEndPoint(0),
          baseLine.GetEndPoint(1));
      }
      else
      {
        pipe = ( DB.Plumbing.Pipe ) docObj;
        pipe.SetSystemType(system.Id);
        ( ( LocationCurve ) pipe.Location ).Curve = baseLine;
      }

      if ( speckleRevitPipe != null )
      {
        TrySetParam(pipe, BuiltInParameter.RBS_START_LEVEL_PARAM, level);
        TrySetParam(pipe, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM, speckleRevitPipe.diameter, speckleRevitPipe.units);

        SetInstanceParameters(pipe, speckleRevitPipe);
      }

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
      if ( !( baseGeometry is Line baseLine ) )
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

      return specklePipe;
    }
  }
}