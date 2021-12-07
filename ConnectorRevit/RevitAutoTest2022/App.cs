#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;

#endregion

namespace RevitAutoTest2022
{
    class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication a)
        {
          a.Idling += (e, args) =>
          {
            Console.WriteLine("Ready!");
          };
          return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }
    }
}
