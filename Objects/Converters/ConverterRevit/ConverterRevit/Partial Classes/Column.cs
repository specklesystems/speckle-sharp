using Objects;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Text;
using Column = Objects.BuiltElements.Column;
using Element = Objects.BuiltElements.Element;
using Line = Objects.Geometry.Line;
using Point = Objects.Geometry.Point;
using Autodesk.Revit.DB.Structure;
using Objects.Revit;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public DB.Element ColumnToNative(Column speckleColumn)
    {
      if (speckleColumn.baseLine == null)
      {
        throw new Exception("Only line based Beams are currently supported.");
      }

      string familyName = "";
      DB.FamilySymbol familySymbol = GetFamilySymbol(speckleColumn); ;
      var baseLine = CurveToNative(speckleColumn.baseLine).get_Item(0);
      DB.Level level = null;
      DB.Level topLevel = null;
      DB.FamilyInstance revitColumn = null;
      var structuralType = StructuralType.NonStructural;
      var isLineBased = true;


      //comes from revit or schema builder, has these props
      var speckleRevitColumn = speckleColumn as RevitColumn;
      if (speckleRevitColumn != null)
      {
        familyName = speckleRevitColumn.family;
        level = LevelToNative(speckleRevitColumn.level);
        topLevel = LevelToNative(speckleRevitColumn.topLevel);
        structuralType = speckleRevitColumn.structural ? StructuralType.Column : StructuralType.NonStructural;
        //non slanted columns are point based
        isLineBased = speckleRevitColumn.isSlanted;
      }
      else
      {
        level = LevelToNative(LevelFromCurve(baseLine));
      }


      var (docObj, stateObj) = GetExistingElementByApplicationId(speckleColumn.applicationId, speckleColumn.speckle_type);
      //try update existing 
      if (docObj != null)
      {
        try
        {
          var revitType = Doc.GetElement(docObj.GetTypeId()) as ElementType;

          // if family changed, tough luck. delete and let us create a new one.
          if (familyName != revitType.FamilyName)
          {
            Doc.Delete(docObj.Id);
          }
          else
          {
            revitColumn = (DB.FamilyInstance)docObj;
            (revitColumn.Location as LocationCurve).Curve = baseLine;


            // check for a type change
            if (!string.IsNullOrEmpty(familyName) && familyName != revitType.Name)
              revitColumn.ChangeTypeId(familySymbol.Id);
          }
        }
        catch
        {
          //something went wrong, re-create it
        }
      }

      if (revitColumn == null && isLineBased)
      {
        revitColumn = Doc.Create.NewFamilyInstance(baseLine, familySymbol, level, structuralType);
      }
      XYZ basePoint = null;
      //try with a point based column
      if (revitColumn == null)
      {
        var start = baseLine.GetEndPoint(0);
        var end = baseLine.GetEndPoint(1);
        basePoint = start.Z < end.Z ? start : end; // pick the lowest
        revitColumn = Doc.Create.NewFamilyInstance(basePoint, familySymbol, level, structuralType);

        //rotate, we know it must be a RevitColumn
        var axis = DB.Line.CreateBound(new XYZ(basePoint.X, basePoint.Y, 0), new XYZ(basePoint.X, basePoint.Y, 1000));
        (revitColumn.Location as LocationPoint).Rotate(axis, speckleRevitColumn.rotation - (revitColumn.Location as LocationPoint).Rotation);
      }

      TrySetParam(revitColumn, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM, level);

      if (speckleRevitColumn != null)
      {
        TrySetParam(revitColumn, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM, topLevel);


        if (speckleRevitColumn.handFlipped != revitColumn.HandFlipped)
          revitColumn.flipHand();

        if (speckleRevitColumn.facingFlipped != revitColumn.FacingFlipped)
          revitColumn.flipFacing();

        SetOffsets(revitColumn, speckleRevitColumn);
        var exclusions = new List<string> { "Base Offset", "Top Offset" };
        SetElementParams(revitColumn, speckleRevitColumn, exclusions);
      }

      return revitColumn;
    }

    /// <summary>
    /// Some families eg columns, need offsets to be set in a specific way
    /// </summary>
    /// <param name="speckleElement"></param>
    /// <param name="familyInstance"></param>
    private void SetOffsets(DB.FamilyInstance familyInstance, RevitColumn speckleRevitColumn)
    {
      var topOffsetParam = familyInstance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
      var baseOffsetParam = familyInstance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
      var baseLevelParam = familyInstance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
      var topLevelParam = familyInstance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);

      if (topLevelParam == null || baseLevelParam == null || baseOffsetParam == null || topOffsetParam == null)
        return;


      var baseOffset = UnitUtils.ConvertToInternalUnits(speckleRevitColumn.baseOffset, baseOffsetParam.DisplayUnitType);
      var topOffset = UnitUtils.ConvertToInternalUnits(speckleRevitColumn.topOffset, baseOffsetParam.DisplayUnitType);

      //these have been set previously
      DB.Level level = Doc.GetElement(baseLevelParam.AsElementId()) as DB.Level;
      DB.Level topLevel = Doc.GetElement(topLevelParam.AsElementId()) as DB.Level;

      //checking if BASE offset needs to be set before or after TOP offset
      if (topLevel != null && topLevel.Elevation + baseOffset <= level.Elevation)
      {
        baseOffsetParam.Set(baseOffset);
        topOffsetParam.Set(topOffset);
      }
      else
      {
        topOffsetParam.Set(topOffset);
        baseOffsetParam.Set(baseOffset);
      }

    }

    public IRevitElement ColumnToSpeckle(DB.FamilyInstance revitColumn)
    {


      //REVIT PARAMS > SPECKLE PROPS
      var baseLevelParam = revitColumn.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
      var topLevelParam = revitColumn.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
      var baseOffsetParam = revitColumn.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
      var topOffsetParam = revitColumn.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);



      var speckleColumn = new RevitColumn();
      speckleColumn.type = Doc.GetElement(revitColumn.GetTypeId()).Name;
      speckleColumn.level = (RevitLevel)ParameterToSpeckle(baseLevelParam);
      speckleColumn.topLevel = (RevitLevel)ParameterToSpeckle(topLevelParam);
      speckleColumn.baseOffset = (double)ParameterToSpeckle(baseOffsetParam);
      speckleColumn.topOffset = (double)ParameterToSpeckle(topOffsetParam);
      speckleColumn.facingFlipped = revitColumn.FacingFlipped;
      speckleColumn.handFlipped = revitColumn.HandFlipped;
      speckleColumn.isSlanted = revitColumn.IsSlantedColumn;
      speckleColumn.structural = revitColumn.StructuralType == StructuralType.Column;

      //geometry
      var baseGeometry = LocationToSpeckle(revitColumn);
      var baseLine = baseGeometry as ICurve;
      //make line from point and height
      if (baseLine == null && baseGeometry is Point basePoint)
      {
        baseLine = new Line(basePoint, new Point(basePoint.x, basePoint.y, speckleColumn.topLevel.elevation + speckleColumn.topOffset));
      }

      if (baseLine == null)
      {
        throw new Exception("Only line based Columns are currently supported.");
      }

      speckleColumn.baseLine = baseLine; //all speckle columns should be line based

      AddCommonRevitProps(speckleColumn, revitColumn);

      if (revitColumn.Location is LocationPoint)
      {
        speckleColumn.rotation = ((LocationPoint)revitColumn.Location).Rotation;
      }

      speckleColumn.displayMesh = MeshUtils.GetElementMesh(revitColumn, Scale);

      return speckleColumn;
    }


  }
}
