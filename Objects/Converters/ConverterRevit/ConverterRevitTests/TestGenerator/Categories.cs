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
          SyncAssertFunc = "AdaptiveComponentEqual"
        } 
      },
      { Beam, new CategoryProperties()
        {
          RevitType = "DB.FamilyInstance",
          SyncAssertFunc = "AssertFamilyInstanceEqual"
        }
      },
      //{ Brep, new CategoryProperties()
      //  {
      //    RevitType = "DB.FamilyInstance",
      //    SyncAssertFunc = "AdaptiveComponentEqual"
      //  }
      //},
      { Column, new CategoryProperties()
        {
          RevitType = "DB.FamilyInstance",
          SyncAssertFunc = "AssertFamilyInstanceEqual"
        }
      },
      { Curve, new CategoryProperties()
        {
          RevitType = "DB.CurveElement",
          SyncAssertFunc = "AssertCurveEqual"
        }
      },
      { DirectShape, new CategoryProperties()
        {
          RevitType = "DB.DirectShape",
          SyncAssertFunc = "DirectShapeEqual"
        }
      },
      { Duct, new CategoryProperties()
        {
          RevitType = "DB.Duct",
          SyncAssertFunc = "AssertDuctEqual"
        }
      },
      { FamilyInstance, new CategoryProperties()
        {
          RevitType = "DB.Element",
          SyncAssertFunc = "AssertNestedEqual"
        }
      },
      { Floor, new CategoryProperties()
        {
          RevitType = "DB.Floor",
          SyncAssertFunc = "null",
          AsyncAssertFunc = "AssertFloorEqual"
        }
      },
      { Opening, new CategoryProperties()
        {
          RevitType = "DB.Element",
          SyncAssertFunc = "AssertOpeningEqual"
        }
      },
      { Pipe, new CategoryProperties()
        {
          RevitType = "DB.Pipe",
          SyncAssertFunc = "AssertPipeEqual"
        }
      },
      { Roof, new CategoryProperties()
        {
          RevitType = "DB.RoofBase",
          SyncAssertFunc = "AssertRoofEqual"
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
          AsyncAssertFunc = "AssertSchedulesEqual",
        }
      },
      { Wall, new CategoryProperties()
        {
          RevitType = "DB.Wall",
          SyncAssertFunc = "AssertWallEqual"
        }
      },
      { Wire, new CategoryProperties()
        {
          RevitType = "DB.Wire",
          SyncAssertFunc = "AssertWireEqual"
        }
      },
    };
  }
}
