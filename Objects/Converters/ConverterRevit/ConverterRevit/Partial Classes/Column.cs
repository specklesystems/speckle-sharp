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
    public DB.Element ColumnToNative(RevitColumn speckleColumn)
    {
      var structuralType = StructuralType.NonStructural;
      Enum.TryParse(speckleColumn.GetMemberSafe("structuralType", "NonStructural"), out structuralType);
      var revitColumn = FamilyInstanceToNative(speckleColumn, structuralType);


      return revitColumn;
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
      speckleColumn.structuralType = revitColumn.StructuralType.ToString();

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
