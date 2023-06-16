using System;
using System.Collections.Generic;
using System.Text;

namespace TestGenerator
{
  internal struct CategoryProperties
  {
    public string RevitType;
    public string AbstractAssert;
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
          AbstractAssert = "AssertAdaptiveComponentsEqual"
        } 
      },
      { Beam, new CategoryProperties()
        {
          RevitType = "DB.FamilyInstance",
          AbstractAssert = "AssertFamilyInstancesEqual"
        }
      },
      //{ Brep, new CategoryProperties()
      //  {
      //    RevitType = "DB.FamilyInstance",
      //    AbstractAssert = "AssertAdaptiveComponentEqual"
      //  }
      //},
      { Column, new CategoryProperties()
        {
          RevitType = "DB.FamilyInstance",
          AbstractAssert = "AssertFamilyInstancesEqual"
        }
      },
      { Curve, new CategoryProperties()
        {
          RevitType = "DB.CurveElement",
          AbstractAssert = "AssertCurvesEqual"
        }
      },
      { DirectShape, new CategoryProperties()
        {
          RevitType = "DB.DirectShape",
          AbstractAssert = "AssertDirectShapesEqual"
        }
      },
      //{ Duct, new CategoryProperties()
      //  {
      //    RevitType = "DB.Mechanical.Duct",
      //    AbstractAssert = "AssertDuctEqual"
      //  }
      //},
      { FamilyInstance, new CategoryProperties()
        {
          RevitType = "DB.Element",
          AbstractAssert = "AssertNestedEqual"
        }
      },
      { Floor, new CategoryProperties()
        {
          RevitType = "DB.Floor",
          AbstractAssert = "null",
          //AAbstractAssert = "AssertFloorsEqual"
        }
      },
      { Opening, new CategoryProperties()
        {
          RevitType = "DB.Element",
          AbstractAssert = "AssertOpeningsEqual"
        }
      },
      //{ Pipe, new CategoryProperties()
      //  {
      //    RevitType = "DB.Plumbing.Pipe",
      //    AbstractAssert = "AssertPipeEqual"
      //  }
      //},
      { Roof, new CategoryProperties()
        {
          RevitType = "DB.RoofBase",
          AbstractAssert = "AssertRoofsEqual"
        }
      },
      { Room, new CategoryProperties()
        {
          RevitType = "null",
          AbstractAssert = "null"
        }
      },
      { Schedule, new CategoryProperties()
        {
          RevitType = "DB.ViewSchedule",
          //AbstractAssert = "null",
          //AAbstractAssert = "AssertSchedulesEqual",
        }
      },
      { Wall, new CategoryProperties()
        {
          RevitType = "DB.Wall",
          AbstractAssert = "AssertWallsEqual"
        }
      },
      { Wire, new CategoryProperties()
        {
          RevitType = "DB.Electrical.Wire",
          AbstractAssert = "AssertWiresEqual"
        }
      },
    };
  }
}
