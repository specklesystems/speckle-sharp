using System;
using System.Collections.Generic;
using System.Text;

namespace TestGenerator
{
  internal struct CategoryProperties
  {
    public string RevitType;
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
          RevitType = "DB.FamilyInstance"
        }
      },
      { Beam, new CategoryProperties()
        {
          RevitType = "DB.FamilyInstance"
        }
      },
      //{ Brep, new CategoryProperties()
      //  {
      //    RevitType = "DB.FamilyInstance"
      //  }
      //},
      { Column, new CategoryProperties()
        {
          RevitType = "DB.FamilyInstance"
        }
      },
      { Curve, new CategoryProperties()
        {
          RevitType = "DB.CurveElement"
        }
      },
      { DirectShape, new CategoryProperties()
        {
          RevitType = "DB.DirectShape"
        }
      },
      //{ Duct, new CategoryProperties()
      //  {
      //    RevitType = "DB.Mechanical.Duct"
      //  }
      //},
      { FamilyInstance, new CategoryProperties()
        {
          RevitType = "DB.Element"
        }
      },
      { Floor, new CategoryProperties()
        {
          RevitType = "DB.Floor",
          //AAbstractAssert = "AssertFloorsEqual"
        }
      },
      { Opening, new CategoryProperties()
        {
          RevitType = "DB.Element"
        }
      },
      //{ Pipe, new CategoryProperties()
      //  {
      //    RevitType = "DB.Plumbing.Pipe"
      //  }
      //},
      { Roof, new CategoryProperties()
        {
          RevitType = "DB.RoofBase"
        }
      },
      { Room, new CategoryProperties()
        {
          RevitType = "null"
        }
      },
      { Schedule, new CategoryProperties()
        {
          RevitType = "DB.ViewSchedule"
        }
      },
      { Wall, new CategoryProperties()
        {
          RevitType = "DB.Wall"
        }
      },
      { Wire, new CategoryProperties()
        {
          RevitType = "DB.Electrical.Wire"
        }
      },
    };
  }
}
