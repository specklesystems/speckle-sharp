using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Column = Objects.BuiltElements.Column;
using DB = Autodesk.Revit.DB;
using Line = Objects.Geometry.Line;
using Point = Objects.Geometry.Point;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject ColumnToNative(Column speckleColumn)
    {
      var docObj = GetExistingElementByApplicationId(speckleColumn.applicationId);
      var appObj = new ApplicationObject(speckleColumn.id, speckleColumn.speckle_type) { applicationId = speckleColumn.applicationId };

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(docObj, appObj))
        return appObj;

      if (speckleColumn.baseLine == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Only line based Beams are currently supported.");
        return appObj;
      }

      var familySymbol = GetElementType<FamilySymbol>(speckleColumn, appObj, out bool isExactMatch);
      if (familySymbol == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed);
        return appObj;
      }

      var baseLine = CurveToNative(speckleColumn.baseLine).get_Item(0);

      // If the start point elevation is higher than the end point elevation, reverse the line.
      if (baseLine.GetEndPoint(0).Z > baseLine.GetEndPoint(1).Z)
        baseLine = DB.Line.CreateBound(baseLine.GetEndPoint(1), baseLine.GetEndPoint(0));

      DB.Level level = null;
      DB.Level topLevel = null;
      DB.FamilyInstance revitColumn = null;
      //var structuralType = StructuralType.Column;
      var isLineBased = true;

      var speckleRevitColumn = speckleColumn as RevitColumn;

      var levelState = ApplicationObject.State.Unknown;
      if (speckleRevitColumn != null)
      {
        level = ConvertLevelToRevit(speckleRevitColumn.level, out levelState);
        topLevel = ConvertLevelToRevit(speckleRevitColumn.topLevel, out levelState);
        //structuralType = speckleRevitColumn.structural ? StructuralType.Column : StructuralType.NonStructural;
        //non slanted columns are point based
        isLineBased = speckleRevitColumn.isSlanted;
      }

      if (level == null)
      {
        level = ConvertLevelToRevit(LevelFromCurve(baseLine), out levelState);
        topLevel = ConvertLevelToRevit(LevelFromPoint(baseLine.GetEndPoint(1)), out levelState);
      }

      //try update existing 

      bool isUpdate = false;
      if (docObj != null)
      {
        try
        {
          var revitType = Doc.GetElement(docObj.GetTypeId()) as ElementType;

          // if family changed, tough luck. delete and let us create a new one.
          if (familySymbol.FamilyName != revitType.FamilyName)
            Doc.Delete(docObj.Id);

          else
          {
            revitColumn = (DB.FamilyInstance)docObj;
            switch (revitColumn.Location)
            {
              case LocationCurve crv:
                crv.Curve = baseLine;
                break;
              case LocationPoint pt:
                pt.Point = baseLine.GetEndPoint(0);
                break;
            }

            // check for a type change
            if (isExactMatch && revitType.Id.IntegerValue != familySymbol.Id.IntegerValue)
            {
              revitColumn.ChangeTypeId(familySymbol.Id);
            }
          }
          isUpdate = true;
        }
        catch { }
      }

      if (revitColumn == null && isLineBased)
      {
        revitColumn = Doc.Create.NewFamilyInstance(baseLine, familySymbol, level, StructuralType.Column);
        if (revitColumn.Symbol.Family.FamilyPlacementType == FamilyPlacementType.CurveDrivenStructural)
        {
          StructuralFramingUtils.DisallowJoinAtEnd(revitColumn, 0);
          StructuralFramingUtils.DisallowJoinAtEnd(revitColumn, 1);
        }
      }

      var start = baseLine.GetEndPoint(0);
      var end = baseLine.GetEndPoint(1);
      var basePoint = start.Z < end.Z ? start : end; // pick the lowest
      //try with a point based column
      if (speckleRevitColumn != null && revitColumn == null && !isLineBased)
        revitColumn = Doc.Create.NewFamilyInstance(basePoint, familySymbol, level, StructuralType.NonStructural);

      //rotate
      if (speckleRevitColumn != null && revitColumn != null)
      {
        var currentRotation = (revitColumn.Location as LocationPoint)?.Rotation;

        if (currentRotation != null && currentRotation != speckleRevitColumn.rotation)
        {
          var axis = DB.Line.CreateBound(new XYZ(basePoint.X, basePoint.Y, 0), new XYZ(basePoint.X, basePoint.Y, 10000));
          var s = (revitColumn.Location as LocationPoint).Rotate(axis, speckleRevitColumn.rotation - (double)currentRotation);
        }
      }

      if (revitColumn == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "revit column was null");
        return appObj;
      }

      TrySetParam(revitColumn, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM, level);
      TrySetParam(revitColumn, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM, topLevel);

      if (speckleRevitColumn != null)
      {
        if (speckleRevitColumn.handFlipped != revitColumn.HandFlipped)
          revitColumn.flipHand();

        if (speckleRevitColumn.facingFlipped != revitColumn.FacingFlipped)
          revitColumn.flipFacing();

        //don't change offset for slanted columns, it's automatic
        if (!isLineBased)
          SetOffsets(revitColumn, speckleRevitColumn, level, topLevel);

        SetInstanceParameters(revitColumn, speckleRevitColumn);
      }

      var state = isUpdate ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
      appObj.Update(status: state, createdId: revitColumn.UniqueId, convertedItem: revitColumn);
      // TODO: nested elements.
      appObj = SetHostedElements(speckleColumn, revitColumn, appObj);
      return appObj;
    }

    /// <summary>
    /// Some families eg columns, need offsets to be set in a specific way. This tries to cover that.
    /// </summary>
    /// <param name="speckleElement"></param>
    /// <param name="familyInstance"></param>
    private void SetOffsets(DB.FamilyInstance familyInstance, RevitColumn speckleRevitColumn, Level level, Level topLevel)
    {
      var topOffsetParam = familyInstance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
      var baseOffsetParam = familyInstance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
      var baseLevelParam = familyInstance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
      var topLevelParam = familyInstance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);

      if (topLevelParam == null || baseLevelParam == null || baseOffsetParam == null || topOffsetParam == null)
        return;

      var baseOffset = ScaleToNative(speckleRevitColumn.baseOffset, speckleRevitColumn.units);
      var topOffset = ScaleToNative(speckleRevitColumn.topOffset, speckleRevitColumn.units);

      // the column length cannot be 0 for even an instance or Revit will throw a fit.
      // Make sure that setting the offset on one side of the column before setting the
      // other side doesn't leave the length of the column as approximately 0
      var colHeightAfterBaseOffset = level.Elevation + baseOffset - topLevel.Elevation;
      var colHeightAfterTopOffset = topLevel.Elevation + topOffset - level.Elevation;

      if (Math.Abs(colHeightAfterBaseOffset) > TOLERANCE)
      {
        baseOffsetParam.Set(baseOffset);
        topOffsetParam.Set(topOffset);
      }
      else if (Math.Abs(colHeightAfterTopOffset) > TOLERANCE)
      {
        topOffsetParam.Set(topOffset);
        baseOffsetParam.Set(baseOffset);
      }
      else
      {
        baseOffsetParam.Set(baseOffset/2); // temporarily set this value to something else so the sides of the column can switch places
        topOffsetParam.Set(topOffset);
        baseOffsetParam.Set(baseOffset);
      }
    }

    public Base ColumnToSpeckle(DB.FamilyInstance revitColumn, out List<string> notes)
    {
      notes = new List<string>();
      var symbol = revitColumn.Document.GetElement(revitColumn.GetTypeId()) as FamilySymbol;

      var speckleColumn = new RevitColumn();
      speckleColumn.family = symbol.FamilyName;
      speckleColumn.type = revitColumn.Document.GetElement(revitColumn.GetTypeId()).Name;
      speckleColumn.level = ConvertAndCacheLevel(revitColumn, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
      speckleColumn.topLevel = ConvertAndCacheLevel(revitColumn, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
      speckleColumn.baseOffset = GetParamValue<double>(revitColumn, BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
      speckleColumn.topOffset = GetParamValue<double>(revitColumn, BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
      speckleColumn.facingFlipped = revitColumn.FacingFlipped;
      speckleColumn.handFlipped = revitColumn.HandFlipped;
      speckleColumn.isSlanted = revitColumn.IsSlantedColumn;
      //speckleColumn.structural = revitColumn.StructuralType == StructuralType.Column;

      //geometry
      var baseGeometry = LocationToSpeckle(revitColumn);
      var baseLine = baseGeometry as ICurve;

      //make line from point and height
      if (baseLine == null && baseGeometry is Point basePoint)
      {
        if (symbol.Family.FamilyPlacementType == FamilyPlacementType.OneLevelBased || symbol.Family.FamilyPlacementType == FamilyPlacementType.WorkPlaneBased)
          return RevitInstanceToSpeckle(revitColumn, out notes, null);

        var elevation = speckleColumn.topLevel.elevation;
        baseLine = new Line(basePoint, new Point(basePoint.x, basePoint.y, elevation + speckleColumn.topOffset, ModelUnits), ModelUnits);
      }

      if (baseLine == null)
        return RevitElementToSpeckle(revitColumn, out notes);

      speckleColumn.baseLine = baseLine; //all speckle columns should be line based

      GetAllRevitParamsAndIds(speckleColumn, revitColumn,
        new List<string> { "FAMILY_BASE_LEVEL_PARAM", "FAMILY_TOP_LEVEL_PARAM", "FAMILY_BASE_LEVEL_OFFSET_PARAM", "FAMILY_TOP_LEVEL_OFFSET_PARAM", "SCHEDULE_BASE_LEVEL_OFFSET_PARAM", "SCHEDULE_TOP_LEVEL_OFFSET_PARAM" });

      if (revitColumn.Location is LocationPoint)
        speckleColumn.rotation = ((LocationPoint)revitColumn.Location).Rotation;

      // structural connection modifiers alter family instance geometry, but the modifiers are view specific
      // so we need to pass in the view we want in order to get the correct geometry
      // TODO: we need to make sure we are passing in the correct view
      var connectionHandlerFilter = new ElementClassFilter(typeof(DB.Structure.StructuralConnectionHandler));
      var options = revitColumn.GetSubelements().Where(o => (BuiltInCategory)o.Category.Id.IntegerValue == DB.BuiltInCategory.OST_StructConnectionModifiers).Any() || revitColumn.GetDependentElements(connectionHandlerFilter).Any() ?
        new Options() { View = Doc.ActiveView, ComputeReferences = true } : SolidDisplayValueOptions;
      speckleColumn.displayValue = GetElementDisplayValue(revitColumn, options);

      return speckleColumn;
    }

  }
}
