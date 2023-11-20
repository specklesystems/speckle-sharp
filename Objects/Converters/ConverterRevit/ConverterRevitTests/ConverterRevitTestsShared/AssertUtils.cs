using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Objects.Converter.Revit;
using RevitSharedResources.Models;
using Speckle.Core.Models;
using Xunit;
using DB = Autodesk.Revit.DB;

namespace ConverterRevitTests
{
  internal static class AssertUtils
  {
    internal static void AdaptiveComponentEqual(DB.FamilyInstance sourceElem, DB.FamilyInstance destElem)
    {
      ElementEqual(sourceElem, destElem);
      EqualParam(sourceElem, destElem, BuiltInParameter.FLEXIBLE_INSTANCE_FLIP);

      var dist = (sourceElem.Location as LocationPoint).Point.DistanceTo((destElem.Location as LocationPoint).Point);

      Assert.True(dist < 0.1);
    }

    internal static void CurveEqual(DB.CurveElement sourceElem, DB.CurveElement destElem)
    {
      ElementEqual(sourceElem, destElem);
      EqualParam(sourceElem, destElem, BuiltInParameter.CURVE_ELEM_LENGTH);
      EqualParam(sourceElem, destElem, BuiltInParameter.BUILDING_CURVE_GSTYLE);

      if (((LocationCurve)sourceElem.Location).Curve.IsBound)
      {
        var sourceEnd = ((LocationCurve)sourceElem.Location).Curve.GetEndPoint(0);
        var destEnd = ((LocationCurve)destElem.Location).Curve.GetEndPoint(0);

        Assert.Equal(sourceEnd.X, destEnd.X, 4);
        Assert.Equal(sourceEnd.Y, destEnd.Y, 4);
        Assert.Equal(sourceEnd.Z, destEnd.Z, 4);
      }
    }

    internal static void DirectShapeEqual(DB.DirectShape sourceElem, DB.DirectShape destElem)
    {
      ElementEqual(sourceElem, destElem);
    }

    internal static void DuctEqual(DB.Mechanical.Duct sourceElem, DB.Mechanical.Duct destElem)
    {
      ElementEqual(sourceElem, destElem);

      EqualParam(sourceElem, destElem, BuiltInParameter.CURVE_ELEM_LENGTH);
      EqualParam(sourceElem, destElem, BuiltInParameter.RBS_START_LEVEL_PARAM);
      EqualParam(sourceElem, destElem, BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM);
      EqualParam(sourceElem, destElem, BuiltInParameter.RBS_CURVE_HEIGHT_PARAM);
      EqualParam(sourceElem, destElem, BuiltInParameter.RBS_CURVE_WIDTH_PARAM);
      EqualParam(sourceElem, destElem, BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
      EqualParam(sourceElem, destElem, BuiltInParameter.RBS_VELOCITY);
    }
    
    internal static void ElementEqual(DB.Element sourceElem, DB.Element destElem)
    {
      Assert.NotNull(sourceElem);
      Assert.NotNull(destElem);
      Assert.Equal(sourceElem.Name, destElem.Name);
      Assert.Equal(sourceElem.Document.GetElement(sourceElem.GetTypeId())?.Name, destElem.Document.GetElement(destElem.GetTypeId())?.Name);
      Assert.Equal(sourceElem.Category.Name, destElem.Category.Name);
    }
    
    internal static void FamilyInstanceEqual(DB.FamilyInstance sourceElem, DB.FamilyInstance destElem)
    {
      ElementEqual(sourceElem, destElem);

      EqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
      EqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
      EqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
      EqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);

      EqualParam(sourceElem, destElem, BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);

      Assert.Equal(sourceElem.FacingFlipped, destElem.FacingFlipped);
      Assert.Equal(sourceElem.HandFlipped, destElem.HandFlipped);
      Assert.Equal(sourceElem.IsSlantedColumn, destElem.IsSlantedColumn);
      Assert.Equal(sourceElem.StructuralType, destElem.StructuralType);

      //rotation
      if (sourceElem.Location is LocationPoint)
        Assert.Equal(((LocationPoint)sourceElem.Location).Rotation, ((LocationPoint)destElem.Location).Rotation);
    }

    internal static async Task FloorEqual(DB.Floor sourceElem, DB.Floor destElem)
    {
      ElementEqual(sourceElem, destElem);

      var slopeArrow = await APIContext.Run(app => {
        return ConverterRevit.GetSlopeArrow(sourceElem);
      }).ConfigureAwait(false);

      if (slopeArrow == null)
      {
        EqualParam(sourceElem, destElem, BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);
      }
      else
      {
        var tailOffset = ConverterRevit.GetSlopeArrowTailOffset(slopeArrow, sourceElem.Document);
        _ = ConverterRevit.GetSlopeArrowHeadOffset(slopeArrow, sourceElem.Document, tailOffset, out var slope);

        var newSlopeArrow = await APIContext.Run(app => {
          return ConverterRevit.GetSlopeArrow(destElem);
        }).ConfigureAwait(false);

        Assert.NotNull(newSlopeArrow);

        var newTailOffset = ConverterRevit.GetSlopeArrowTailOffset(slopeArrow, sourceElem.Document);
        _ = ConverterRevit.GetSlopeArrowHeadOffset(slopeArrow, sourceElem.Document, tailOffset, out var newSlope);
        Assert.Equal(slope, newSlope);

        var sourceOffset = ConverterRevit.GetParamValue<double>(sourceElem, BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);
        Assert.Equal(sourceOffset + tailOffset, ConverterRevit.GetParamValue<double>(destElem, BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM));
      }

      EqualParam(sourceElem, destElem, BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL);
      EqualParam(sourceElem, destElem, BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM);
    }

    internal static void NestedEqual(DB.Element sourceElem, DB.Element destElem)
    {
      ElementEqual(sourceElem, destElem);

      //family instance
      EqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_LEVEL_PARAM);
      EqualParam(sourceElem, destElem, BuiltInParameter.INSTANCE_ELEVATION_PARAM);
      EqualParam(sourceElem, destElem, BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);

      EqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
      EqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
      EqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
      EqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
      EqualParam(sourceElem, destElem, BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);


      if (sourceElem is FamilyInstance fi && fi.Host != null && destElem is FamilyInstance fi2)
      {
        Assert.Equal(fi.Host.Name, fi2.Host.Name);
      }

      //rotation
      //for some reasons, rotation of hosted families stopped working in 2021.1 ...?
      if (sourceElem.Location is LocationPoint && sourceElem is FamilyInstance fi3 && fi3.Host == null)
        Assert.Equal(((LocationPoint)sourceElem.Location).Rotation, ((LocationPoint)destElem.Location).Rotation, 3);


      //walls
      EqualParam(sourceElem, destElem, BuiltInParameter.WALL_USER_HEIGHT_PARAM);
      EqualParam(sourceElem, destElem, BuiltInParameter.WALL_BASE_OFFSET);
      EqualParam(sourceElem, destElem, BuiltInParameter.WALL_TOP_OFFSET);
      EqualParam(sourceElem, destElem, BuiltInParameter.WALL_BASE_CONSTRAINT);
      EqualParam(sourceElem, destElem, BuiltInParameter.WALL_HEIGHT_TYPE);
      EqualParam(sourceElem, destElem, BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT);
    }

    internal static void OpeningEqual(DB.Element sourceElem, DB.Element destElem)
    {
      if (!(sourceElem is DB.Opening))
        return;

      ElementEqual(sourceElem, destElem);

      EqualParam(sourceElem, destElem, BuiltInParameter.WALL_BASE_CONSTRAINT);
      EqualParam(sourceElem, destElem, BuiltInParameter.WALL_HEIGHT_TYPE);
      EqualParam(sourceElem, destElem, BuiltInParameter.WALL_USER_HEIGHT_PARAM);
    }

    internal static void EqualParam(DB.Element expected, DB.Element actual, BuiltInParameter param)
    {
      var expecedParam = expected.get_Parameter(param);
      if (expecedParam == null)
        return;

      switch (expecedParam.StorageType)
      {
        case StorageType.Double:
          Assert.Equal(expecedParam.AsDouble(), actual.get_Parameter(param).AsDouble(), 4);
          break;
        case StorageType.Integer:
          Assert.Equal(expecedParam.AsInteger(), actual.get_Parameter(param).AsInteger());
          break;
        case StorageType.String:
          Assert.Equal(expecedParam.AsString(), actual.get_Parameter(param).AsString());
          break;
        case StorageType.ElementId:
          {
            var e1 = expected.Document.GetElement(expecedParam.AsElementId());
            var e2 = actual.Document.GetElement(actual.get_Parameter(param).AsElementId());
            if (e1 is Level l1 && e2 is Level l2)
              Assert.Equal(l1.Elevation, l2.Elevation, 3);
            else if (e1 != null && e2 != null)
              Assert.Equal(e1.Name, e2.Name);
            break;
          }
        case StorageType.None:
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    internal static void PipeEqual(DB.Plumbing.Pipe sourceElem, DB.Plumbing.Pipe destElem)
    {
      ElementEqual(sourceElem, destElem);

      EqualParam(sourceElem, destElem, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
      EqualParam(sourceElem, destElem, BuiltInParameter.CURVE_ELEM_LENGTH);
      EqualParam(sourceElem, destElem, BuiltInParameter.RBS_START_LEVEL_PARAM);
      EqualParam(sourceElem, destElem, BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM);
    }

    internal static void RoofEqual(DB.RoofBase sourceElem, DB.RoofBase destElem)
    {
      ElementEqual(sourceElem, destElem);

      EqualParam(sourceElem, destElem, BuiltInParameter.ROOF_SLOPE);
      EqualParam(sourceElem, destElem, BuiltInParameter.ROOF_BASE_LEVEL_PARAM);
      EqualParam(sourceElem, destElem, BuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM);
      EqualParam(sourceElem, destElem, BuiltInParameter.ROOF_UPTO_LEVEL_PARAM);
    }

    internal static async Task ScheduleEqual(DB.ViewSchedule sourceElem, DB.ViewSchedule destElem)
    {
      ElementEqual(sourceElem, destElem);

      var sourceValueList = await APIContext.Run(app =>
      {
        return GetTextValuesFromSchedule(sourceElem);
      }).ConfigureAwait(false);

      var destValueList = await APIContext.Run(app =>
      {
        return GetTextValuesFromSchedule(destElem);
      }).ConfigureAwait(false);

      var index = 0;
      for (var i = 0; i < sourceValueList.Count; i++)
      {
        try
        {
          // you can't unassign parameter values in Revit, so just ignore those
          var emptyIndicies = Enumerable.Range(0, sourceValueList[i].Count)
             .Where(j => string.IsNullOrWhiteSpace(sourceValueList[i][j]))
             .ToList();

          if (emptyIndicies.Any())
          {
            for (var j = emptyIndicies.Count - 1; j >= 0; j--)
            {
              sourceValueList[i].RemoveAt(emptyIndicies[j]);
              destValueList[i].RemoveAt(emptyIndicies[j]);
            }
          }
          Assert.Equal(sourceValueList[i], destValueList[i]);
        }
        catch (Exception ex)
        {
          throw;
        }
        index++;
      }
    }

    private static List<List<string>> GetTextValuesFromSchedule(ViewSchedule revitSchedule)
    {
      var originalTableIds = new FilteredElementCollector(revitSchedule.Document, revitSchedule.Id)
        .ToElementIds();
      var values = new List<List<string>>();
      foreach (var rowInfo in RevitScheduleUtils.ScheduleRowIteration(revitSchedule))
      {
        if (rowInfo.tableSection == SectionType.Header)
        {
          continue;
        }
        if (!ConverterRevit.ElementApplicationIdsInRow(rowInfo.rowIndex, rowInfo.section, originalTableIds, revitSchedule, rowInfo.tableSection).Any())
        {
          continue;
        }

        var innerList = new List<string>();
        for (var columnIndex = 0; columnIndex < rowInfo.columnCount; columnIndex++)
        {
          innerList.Add(revitSchedule.GetCellText(rowInfo.tableSection, rowInfo.rowIndex, columnIndex));
        }
        values.Add(innerList);
      }

      return values;
    }

    internal static void ValidSpeckleElement(DB.Element elem, Base spkElem)
    {
      Assert.NotNull(elem);
      Assert.NotNull(spkElem);
      Assert.NotNull(spkElem["speckle_type"]);
      Assert.NotNull(spkElem["applicationId"]);

      SpeckleUtils.CustomAssertions(elem, spkElem);
    }

    internal static void WallEqual(DB.Wall sourceElem, DB.Wall destElem)
    {
      ElementEqual(sourceElem, destElem);

      EqualParam(sourceElem, destElem, BuiltInParameter.WALL_USER_HEIGHT_PARAM);
      EqualParam(sourceElem, destElem, BuiltInParameter.WALL_BASE_OFFSET);
      EqualParam(sourceElem, destElem, BuiltInParameter.WALL_TOP_OFFSET);
      EqualParam(sourceElem, destElem, BuiltInParameter.WALL_BASE_CONSTRAINT);
      EqualParam(sourceElem, destElem, BuiltInParameter.WALL_HEIGHT_TYPE);
      EqualParam(sourceElem, destElem, BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT);
    }

    internal static void WireEqual(DB.Electrical.Wire sourceElem, DB.Electrical.Wire destElem)
    {
      ElementEqual(sourceElem, destElem);

      EqualParam(sourceElem, destElem, BuiltInParameter.RBS_ELEC_WIRE_TYPE);
      EqualParam(sourceElem, destElem, BuiltInParameter.FABRIC_WIRE_LENGTH);
      EqualParam(sourceElem, destElem, BuiltInParameter.RBS_ELEC_WIRE_ELEVATION);
    }
  }
}
