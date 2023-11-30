using System.IO;
using System.Linq;

namespace ConverterRevitTests;

public static class Globals
{
  public static string GetTestModel(string filename)
  {
    var path = string.Empty;
#if REVIT2021
    path = Path.Combine(GetTestModelFolderLocation(Directory.GetCurrentDirectory(), "2021"), filename);
#elif REVIT2022
    path = Path.Combine(GetTestModelFolderLocation(Directory.GetCurrentDirectory(), "2022"), filename);
#elif REVIT2023
    path = Path.Combine(GetTestModelFolderLocation(Directory.GetCurrentDirectory(), "2023"), filename);
#endif
    return path;
  }

  public static string GetTestModelOfCategory(string category, string filename)
  {
    var path = string.Empty;
#if REVIT2021
    path = Path.Combine(GetTestModelFolderLocation(Directory.GetCurrentDirectory(), "2021"), category, filename);
#elif REVIT2022
    path = Path.Combine(GetTestModelFolderLocation(Directory.GetCurrentDirectory(), "2022"), category, filename);
#elif REVIT2023
    path = Path.Combine(GetTestModelFolderLocation(Directory.GetCurrentDirectory(), "2023"), category, filename);
#endif
    return path;
  }

  /// <summary>
  /// This is the same method in TestGenerator.Globals.
  /// TODO: Consolidate
  /// </summary>
  /// <param name="directoryStringInSpeckleSharp"></param>
  /// <param name="year"></param>
  /// <returns></returns>
  public static string GetTestModelFolderLocation(string directoryStringInSpeckleSharp, string year)
  {
    var assemblyLocationList = directoryStringInSpeckleSharp.Split('\\').ToList();

    for (var i = assemblyLocationList.Count - 1; i >= 0; i--)
    {
      var folderName = assemblyLocationList[i];
      assemblyLocationList.RemoveAt(i);
      if (folderName == "speckle-sharp")
      {
        break;
      }
    }
    assemblyLocationList.Add("speckle-sharp-test-models");
    assemblyLocationList.Add("Revit");

    assemblyLocationList.Add(year);
    var testFolderLocation = string.Join("\\", assemblyLocationList);
    return testFolderLocation;
  }
}
