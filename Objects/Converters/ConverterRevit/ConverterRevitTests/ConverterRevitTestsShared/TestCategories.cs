using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace ConverterRevitTests;

internal static class TestCategories
{
  public const string AdaptiveComponent = "adaptivecomponent";
  public const string Beam = "beam";
  public const string Brep = "brep";
  public const string Column = "column";
  public const string Curve = "curve";
  public const string DirectShape = "directshape";
  public const string Duct = "duct";
  public const string FamilyInstance = "familyinstance";
  public const string Floor = "floor";
  public const string Opening = "opening";
  public const string Pipe = "pipe";
  public const string Roof = "roof";
  public const string Room = "room";
  public const string Schedule = "schedule";
  public const string Wall = "wall";
  public const string Wire = "wire";

  public static Dictionary<string, List<BuiltInCategory>> CategoriesDict =
    new()
    {
      {
        AdaptiveComponent,
        new List<BuiltInCategory>() { BuiltInCategory.OST_GenericModel }
      },
      {
        Beam,
        new List<BuiltInCategory>() { BuiltInCategory.OST_StructuralFraming }
      },
      {
        Brep,
        new List<BuiltInCategory>() { BuiltInCategory.OST_Mass }
      },
      {
        Column,
        new List<BuiltInCategory>() { BuiltInCategory.OST_Columns, BuiltInCategory.OST_StructuralColumns }
      },
      {
        Curve,
        new List<BuiltInCategory>() { BuiltInCategory.OST_Lines, BuiltInCategory.OST_RoomSeparationLines }
      },
      {
        DirectShape,
        new List<BuiltInCategory>() { BuiltInCategory.OST_GenericModel }
      },
      {
        Duct,
        new List<BuiltInCategory>() { BuiltInCategory.OST_DuctCurves }
      },
      {
        FamilyInstance,
        new List<BuiltInCategory>()
        {
          BuiltInCategory.OST_Furniture,
          BuiltInCategory.OST_Doors,
          BuiltInCategory.OST_Walls,
          BuiltInCategory.OST_Windows,
          BuiltInCategory.OST_CeilingOpening,
          BuiltInCategory.OST_ColumnOpening,
          BuiltInCategory.OST_FloorOpening,
          BuiltInCategory.OST_ShaftOpening,
          BuiltInCategory.OST_StructuralFramingOpening,
          BuiltInCategory.OST_SWallRectOpening,
          BuiltInCategory.OST_ArcWallRectOpening,
          BuiltInCategory.OST_FloorOpening,
          BuiltInCategory.OST_SWallRectOpening,
          BuiltInCategory.OST_Floors
        }
      },
      {
        Floor,
        new List<BuiltInCategory>() { BuiltInCategory.OST_Floors }
      },
      {
        Opening,
        new List<BuiltInCategory>()
        {
          BuiltInCategory.OST_CeilingOpening,
          BuiltInCategory.OST_ColumnOpening,
          BuiltInCategory.OST_FloorOpening,
          BuiltInCategory.OST_ShaftOpening,
          BuiltInCategory.OST_StructuralFramingOpening,
          BuiltInCategory.OST_SWallRectOpening,
          BuiltInCategory.OST_ArcWallRectOpening,
          BuiltInCategory.OST_Walls,
          BuiltInCategory.OST_Floors,
          BuiltInCategory.OST_Ceilings,
          BuiltInCategory.OST_RoofOpening,
          BuiltInCategory.OST_Roofs
        }
      },
      {
        Pipe,
        new List<BuiltInCategory>() { BuiltInCategory.OST_PipeCurves }
      },
      {
        Roof,
        new List<BuiltInCategory>() { BuiltInCategory.OST_Roofs }
      },
      {
        Room,
        new List<BuiltInCategory>() { BuiltInCategory.OST_Rooms }
      },
      {
        Schedule,
        new List<BuiltInCategory>() { BuiltInCategory.OST_Schedules }
      },
      {
        Wall,
        new List<BuiltInCategory>() { BuiltInCategory.OST_Walls }
      },
      {
        Wire,
        new List<BuiltInCategory>() { BuiltInCategory.OST_Wire }
      },
    };
}
