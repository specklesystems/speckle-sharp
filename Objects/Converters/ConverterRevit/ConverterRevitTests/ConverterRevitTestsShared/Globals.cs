using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConverterRevitTests
{
  public static class Globals
  {
    public static string GetTestModel(string filename)
    {
#if REVIT2021
      var path = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "TestModels", "2021", filename);
#elif REVIT2022
#elif REVIT2023
      var path = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "TestModels", "2023", filename);
#endif
      return path;

    }
  }
}
