using Objects;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Text;
using Column = Objects.Column;
using Element = Objects.Element;
using Level = Objects.Level;
using Mesh = Objects.Geometry.Mesh;
using Autodesk.Revit.DB.Structure;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public DB.Element ColumnToNative(Column speckleColumn)
    {
      var structuralType = StructuralType.NonStructural;
      Enum.TryParse(speckleColumn.GetMemberSafe("structuralType", "NonStructural"), out structuralType);
      var revitColumn = FamilyInstanceToNative(speckleColumn, structuralType);


      return revitColumn;
    }

    public Column ColumnToSpeckle(FamilyInstance revitColumn)
    {
      //REVIT PARAMS > SPECKLE PROPS
      var baseLevelParam = revitColumn.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
      var topLevelParam = revitColumn.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
      var baseOffsetParam = revitColumn.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
      var topOffsetParam = revitColumn.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);


      var speckleColumn = new Column();
      speckleColumn.type = Doc.GetElement(revitColumn.GetTypeId()).Name;
      speckleColumn.baseGeometry = LocationToSpeckle(revitColumn); //all speckle columns should be line based
      speckleColumn.level = (Level)ParameterToSpeckle(baseLevelParam);
      speckleColumn["topLevel"] = (Level)ParameterToSpeckle(topLevelParam);
      speckleColumn["baseOffset"] = (double)ParameterToSpeckle(baseOffsetParam);
      speckleColumn["topOffset"] = (double)ParameterToSpeckle(topOffsetParam);
      speckleColumn["facingFlipped"] = revitColumn.FacingFlipped;
      speckleColumn["handFlipped"] = revitColumn.HandFlipped;
      speckleColumn["isSlanted"] = revitColumn.IsSlantedColumn;
      speckleColumn["structuralType"] = revitColumn.StructuralType.ToString();

      AddCommonRevitProps(speckleColumn, revitColumn);

      if (revitColumn.Location is LocationPoint)
      {
        speckleColumn["rotation"] = ((LocationPoint)revitColumn.Location).Rotation;
      }
     
      speckleColumn.displayMesh = MeshUtils.GetElementMesh(revitColumn, Scale);

      return speckleColumn;
    }


  }
}
