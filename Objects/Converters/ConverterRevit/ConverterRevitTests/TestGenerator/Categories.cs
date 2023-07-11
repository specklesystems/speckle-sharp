using System;
using System.Collections.Generic;
using System.Text;

namespace TestGenerator
{
  internal struct CategoryProperties
  {
    public string RevitType;
    public string SyncAssertFunc;
    public string AsyncAssertFunc;
  }
  internal class Categories
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

    public static Dictionary<string, CategoryProperties> CategoriesDict = new()
    {
      { AdaptiveComponent, new CategoryProperties()
        {
          RevitType = "DB.FamilyInstance",
          SyncAssertFunc = "AssertUtils.AdaptiveComponentEqual"
        } 
      },
      { Beam, new CategoryProperties()
        {
          RevitType = "DB.FamilyInstance",
          SyncAssertFunc = "AssertUtils.FamilyInstanceEqual"
        }
      },
      //{ Brep, new CategoryProperties()
      //  {
      //    RevitType = "DB.FamilyInstance",
      //    SyncAssertFunc = "AssertUtils.AdaptiveComponentEqual"
      //  }
      //},
      { Column, new CategoryProperties()
        {
          RevitType = "DB.FamilyInstance",
          SyncAssertFunc = "AssertUtils.FamilyInstanceEqual"
        }
      },
      { Curve, new CategoryProperties()
        {
          RevitType = "DB.CurveElement",
          SyncAssertFunc = "AssertUtils.CurveEqual"
        }
      },
      { DirectShape, new CategoryProperties()
        {
          RevitType = "DB.DirectShape",
          SyncAssertFunc = "AssertUtils.DirectShapeEqual"
        }
      },
      //{ Duct, new CategoryProperties()
      //  {
      //    RevitType = "DB.Mechanical.Duct",
      //    SyncAssertFunc = "AssertUtils.DuctEqual"
      //  }
      //},
      { FamilyInstance, new CategoryProperties()
        {
          RevitType = "DB.Element",
          SyncAssertFunc = "AssertUtils.NestedEqual"
        }
      },
      { Floor, new CategoryProperties()
        {
          RevitType = "DB.Floor",
          SyncAssertFunc = "null",
          AsyncAssertFunc = "AssertUtils.FloorEqual"
        }
      },
      { Opening, new CategoryProperties()
        {
          RevitType = "DB.Element",
          SyncAssertFunc = "AssertUtils.OpeningEqual"
        }
      },
      //{ Pipe, new CategoryProperties()
      //  {
      //    RevitType = "DB.Plumbing.Pipe",
      //    SyncAssertFunc = "AssertUtils.PipeEqual"
      //  }
      //},
      { Roof, new CategoryProperties()
        {
          RevitType = "DB.RoofBase",
          SyncAssertFunc = "AssertUtils.RoofEqual"
        }
      },
      { Room, new CategoryProperties()
        {
          RevitType = "null",
          SyncAssertFunc = "null"
        }
      },
      { Schedule, new CategoryProperties()
        {
          RevitType = "DB.ViewSchedule",
          //SyncAssertFunc = "null",
          AsyncAssertFunc = "AssertUtils.ScheduleEqual",
        }
      },
      { Wall, new CategoryProperties()
        {
          RevitType = "DB.Wall",
          SyncAssertFunc = "AssertUtils.WallEqual"
        }
      },
      { Wire, new CategoryProperties()
        {
          RevitType = "DB.Electrical.Wire",
          SyncAssertFunc = "AssertUtils.WireEqual"
        }
      },
    };
  }
}
