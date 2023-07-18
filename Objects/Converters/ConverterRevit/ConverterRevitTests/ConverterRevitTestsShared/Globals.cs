using System.IO;

namespace ConverterRevitTests
{
  public static class Globals
  {
    public static string GetTestModel(string filename)
    {
      var path = string.Empty;
#if REVIT2021
      path = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "TestModels", "2021", filename);
#elif REVIT2022
      path = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "TestModels", "2022", filename);
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
      path = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "TestModels", "2022", category, filename);
#elif REVIT2023
      path = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "TestModels", "2023", category, filename);
#endif
      return path;

    }
  }
}
