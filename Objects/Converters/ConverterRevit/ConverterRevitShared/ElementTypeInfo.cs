using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Objects.BuiltElements;
using RevitSharedResources.Interfaces;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using OSG = Objects.Structural.Geometry;

namespace Objects.Converter.Revit
{
  public class ElementTypeInfo : IElementTypeInfo<BuiltInCategory>
  {
    protected ElementTypeInfo(string name, Type instanceType, Type familyType, List<BuiltInCategory> categories)
    {
      CategoryName = name;
      ElementInstanceType = instanceType;
      ElementTypeType = familyType;
      BuiltInCategories = categories;
    }
    public string CategoryName { get; }
    public Type ElementInstanceType { get; }
    public Type ElementTypeType { get; }
    public List<BuiltInCategory> BuiltInCategories { get; }
    public static ElementTypeInfo Column { get; } = new(
      nameof(Column),
      typeof(FamilyInstance),
      typeof(FamilySymbol),
      new List<BuiltInCategory>
      {
        BuiltInCategory.OST_Columns,
        BuiltInCategory.OST_StructuralColumns
      });

    public static ElementTypeInfo Duct { get; } = new(
      nameof(Duct),
      typeof(DB.Mechanical.Duct),
      typeof(DB.Mechanical.FlexDuctType),
      new List<BuiltInCategory>
      {
        BuiltInCategory.OST_DuctCurves,
        BuiltInCategory.OST_FlexDuctCurves
      });

    public static ElementTypeInfo Floor { get; } = new(
      nameof(Floor),
      typeof(DB.Floor),
      typeof(DB.FloorType),
      new List<BuiltInCategory>
      {
        BuiltInCategory.OST_Floors
      });

    //public static ElementTypeInfo Material { get; } = new(
    //  nameof(Material), 
    //  typeof(DB.Material), 
    //  null,
    //  new List<BuiltInCategory> 
    //  { 
    //    BuiltInCategory.OST_Materials, 
    //    BuiltInCategory.OST_PipeMaterials,
    //    BuiltInCategory.OST_WireMaterials
    //  });

    public static ElementTypeInfo Pipe { get; } = new(
      nameof(Pipe),
      typeof(DB.Plumbing.Pipe),
      typeof(DB.Plumbing.FlexPipeType),
      new List<BuiltInCategory>
      {
        BuiltInCategory.OST_PipeCurves,
        BuiltInCategory.OST_FlexPipeCurves
      });

    public static ElementTypeInfo Roof { get; } = new(
      nameof(Roof),
      typeof(DB.RoofBase),
      typeof(DB.RoofType),
      new List<BuiltInCategory>
      {
        BuiltInCategory.OST_Roofs,
      });

    public static ElementTypeInfo StructuralFraming { get; } = new(
      nameof(StructuralFraming),
      typeof(DB.FamilyInstance),
      typeof(DB.FamilySymbol),
      new List<BuiltInCategory>
      {
        BuiltInCategory.OST_StructuralFraming
      });

    public static ElementTypeInfo Wall { get; } = new(
      nameof(Wall),
      typeof(DB.Wall),
      typeof(DB.WallType),
      new List<BuiltInCategory>
      {
        BuiltInCategory.OST_Walls
      });

    public static ElementTypeInfo Undefined { get; } = new (
      nameof(Undefined),
      null,
      null,
      new List<BuiltInCategory>()
      );

    public static ElementTypeInfo GetElementTypeInfoOfSpeckleObject(Base @base)
    {
      return @base switch
      {
        Objects.BuiltElements.Column => Column,
        Beam => StructuralFraming,
        Brace => StructuralFraming,
        Objects.BuiltElements.Duct => Duct,
        OSG.Element1D e when e.type == OSG.ElementType1D.Column => Column,
        OSG.Element1D e when e.type == OSG.ElementType1D.Beam => StructuralFraming,
        OSG.Element1D e when e.type == OSG.ElementType1D.Brace => StructuralFraming,
        OSG.Element2D => Floor,
        Objects.BuiltElements.Floor => Floor,
        //Objects.Other.Material => Material,
        Objects.BuiltElements.Pipe => Pipe,
        Objects.BuiltElements.Roof => Roof,
        Objects.BuiltElements.Wall => Wall,
        _ => Undefined
      };
    }

    public static ElementTypeInfo GetElementTypeInfoOfCategory(string categoryName)
    {
      var match = typeof(ElementTypeInfo)
        .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
        .Where(field => field.FieldType == typeof(ElementTypeInfo))
        .Select(field => field.GetValue(null) as ElementTypeInfo)
        .FirstOrDefault(typeInfo => typeInfo?.CategoryName == categoryName);

      if (match != null) return match;

      categoryName = categoryName.ToLower();
      switch (categoryName)
      {

        case string a when a.Contains("beam"):
        case string b when b.Contains("brace"):
        case string c when c.Contains("framing"):
          return StructuralFraming;

        case string a when a.Contains("column"):
          return Column;
        case string a when a.Contains("duct"):
          return Duct;
        //case string a when a.Contains("material"):
        //  return Material;
        case string a when a.Contains("floor"):
          return Floor;
        case string a when a.Contains("pipe"):
          return Pipe;
        case string a when a.Contains("roof"):
          return Roof;
        case string a when a.Contains("wall"):
          return Wall;
      }
      return Undefined;
    }
  }
}
