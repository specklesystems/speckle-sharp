using System.IO;
using System.Linq;

namespace TestGenerator
{
  public static class Globals
  {
    /// <summary>
    /// This is the same method in ConverterRevitTests.Globals
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
        if (folderName == "speckle-sharp") break;
      }
      assemblyLocationList.Add("speckle-sharp-test-models");
      assemblyLocationList.Add("Revit");

      assemblyLocationList.Add(year);
      var testFolderLocation = string.Join("\\", assemblyLocationList);
      return testFolderLocation;
    }
  }
}
