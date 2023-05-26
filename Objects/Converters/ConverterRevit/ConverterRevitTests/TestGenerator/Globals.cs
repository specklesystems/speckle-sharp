using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TestGenerator
{
  public static class Globals
  {
    public static string TestModelsFolderPath(string basePath)
    {
      return Path.Combine(basePath, "..", "..", "TestModels");
    }

    public static string TestModelsFolderForRevitVersion(string basePath, string year)
    {
      return Path.Combine(TestModelsFolderPath(basePath), year);
    }

    public static string GetTestModel(string filename)
    {
      var path = string.Empty;
#if REVIT2021
      path = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "TestModels", "2021", filename);
#elif REVIT2022
#elif REVIT2023
      path = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "TestModels", "2023", filename);
#endif
      return path;

    }
    public static string GetTestModelOfCategory(string category, string filename)
    {
      var path = string.Empty;
#if REVIT2021
      path = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "TestModels", "2021", category, filename);
#elif REVIT2022
#elif REVIT2023
      path = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "TestModels", "2023", category, filename);
#endif
      return path;
    }
  }
}
