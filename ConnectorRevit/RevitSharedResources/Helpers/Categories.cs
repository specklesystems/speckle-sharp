using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;

namespace RevitSharedResources.Helpers;

/// <summary>
/// Contains all shared logic related to Categories between Converter and Connector
/// </summary>
public static class Categories
{

  public static Dictionary<string, RevitCategoryInfo> All { get; }

  static Categories()
  {
    All = new(StringComparer.OrdinalIgnoreCase)
    {
      { nameof(CableTray), CableTray },
      { nameof(Ceiling), Ceiling },
      { nameof(Column), Column },
      { nameof(Conduit), Conduit },
      { nameof(Door), Door },
      { nameof(Duct), Duct },
      { nameof(FamilyInstance), FamilyInstance },
      { nameof(Floor), Floor },
      { nameof(Furniture), Furniture },
      { nameof(Pipe), Pipe },
      { nameof(PlumbingFixture), PlumbingFixture},
      { nameof(Roof), Roof },
      { nameof(Railing), Railing },
      { nameof(StructuralFraming), StructuralFraming },
      { nameof(Wall), Wall },
      { nameof(Window), Window },
      { nameof(Wire), Wire },
      { nameof(Undefined), Undefined },
    };
  }
  public static RevitCategoryInfo CableTray { get; } = new(
    nameof(CableTray),
    typeof(DB.Electrical.CableTray),
    typeof(DB.Electrical.CableTrayType),
    new List<BuiltInCategory>()
  );
  public static RevitCategoryInfo Ceiling { get; } = new(
    nameof(Ceiling),
    typeof(DB.Ceiling),
    typeof(CeilingType),
    new List<BuiltInCategory>()
  );
  public static RevitCategoryInfo Column { get; } = new(
    nameof(Column),
    typeof(FamilyInstance),
    typeof(FamilySymbol),
    new List<BuiltInCategory>
    {
        BuiltInCategory.OST_Columns,
        BuiltInCategory.OST_StructuralColumns
    });
  public static RevitCategoryInfo Conduit { get; } = new(
    nameof(Conduit),
    typeof(DB.Electrical.Conduit),
    typeof(DB.Electrical.ConduitType),
    new List<BuiltInCategory>()
  );
  public static RevitCategoryInfo Door { get; } = new(
    nameof(Door),
    typeof(DB.FamilyInstance),
    typeof(DB.FamilySymbol),
    new List<BuiltInCategory>
    {
        BuiltInCategory.OST_Doors
    });
  public static RevitCategoryInfo Duct { get; } = new(
    nameof(Duct),
    typeof(DB.Mechanical.Duct),
    typeof(DB.MEPCurveType),
    new List<BuiltInCategory>
    {
        BuiltInCategory.OST_DuctCurves,
        BuiltInCategory.OST_FlexDuctCurves
    });
  public static RevitCategoryInfo FamilyInstance { get; } = new(
    nameof(FamilyInstance),
    typeof(DB.FamilyInstance),
    typeof(DB.FamilySymbol),
    new List<BuiltInCategory>()
    );
  public static RevitCategoryInfo Floor { get; } = new(
    nameof(Floor),
    typeof(DB.Floor),
    typeof(DB.FloorType),
    new List<BuiltInCategory>
    {
        BuiltInCategory.OST_Floors
    });
  public static RevitCategoryInfo Furniture { get; } = new(
    nameof(Furniture),
    typeof(DB.FamilyInstance),
    typeof(DB.FamilySymbol),
    new List<BuiltInCategory>
    {
        BuiltInCategory.OST_Furniture
    });
  //public static RevitCategoryInfo Material { get; } = new(
  //  nameof(Material), 
  //  typeof(DB.Material), 
  //  null,
  //  new List<BuiltInCategory> 
  //  { 
  //    BuiltInCategory.OST_Materials, 
  //    BuiltInCategory.OST_PipeMaterials,
  //    BuiltInCategory.OST_WireMaterials
  //  });
  public static RevitCategoryInfo Pipe { get; } = new(
    nameof(Pipe),
    typeof(DB.Plumbing.Pipe),
    typeof(DB.MEPCurveType),
    new List<BuiltInCategory>
    {
        BuiltInCategory.OST_PipeCurves,
        BuiltInCategory.OST_FlexPipeCurves
    });
  public static RevitCategoryInfo PlumbingFixture { get; } = new(
    nameof(PlumbingFixture),
    typeof(DB.FamilyInstance),
    typeof(DB.FamilySymbol),
    new List<BuiltInCategory>
    {
        BuiltInCategory.OST_PlumbingFixtures
    });
  public static RevitCategoryInfo Roof { get; } = new(
    nameof(Roof),
    typeof(DB.RoofBase),
    typeof(DB.RoofType),
    new List<BuiltInCategory>
    {
        BuiltInCategory.OST_Roofs,
    });
  public static RevitCategoryInfo Railing { get; } = new(
    nameof(Railing),
    typeof(DB.Architecture.Railing),
    typeof(DB.Architecture.RailingType),
    new List<BuiltInCategory>()
    );
  public static RevitCategoryInfo StructuralFraming { get; } = new(
    nameof(StructuralFraming),
    typeof(DB.FamilyInstance),
    typeof(DB.FamilySymbol),
    new List<BuiltInCategory>
    {
        BuiltInCategory.OST_StructuralFraming
    },
    new List<string> { "beam", "brace", "framing" });
  public static RevitCategoryInfo Wall { get; } = new(
    nameof(Wall),
    typeof(DB.Wall),
    typeof(DB.WallType),
    new List<BuiltInCategory>
    {
        BuiltInCategory.OST_Walls
    });
  public static RevitCategoryInfo Window { get; } = new(
    nameof(Window),
    typeof(DB.FamilyInstance),
    typeof(DB.FamilySymbol),
    new List<BuiltInCategory>
    {
        BuiltInCategory.OST_Windows
    });
  public static RevitCategoryInfo Wire { get; } = new(
    nameof(Wire),
    typeof(DB.Electrical.Wire),
    typeof(DB.Electrical.WireType),
    new List<BuiltInCategory>()
    );
  public static RevitCategoryInfo Undefined { get; } = new(
    nameof(Undefined),
    null,
    null,
    new List<BuiltInCategory>()
    );
}
