using Autodesk.Revit.UI;

namespace Speckle.ConnectorRevitDUI3.Utils;

public static class RevitAppProvider
{
  /// <summary>
  /// This property gets initialized in the plugin entry, on app initialization.
  /// We should be able to use this in any place where we need the revit app! 
  /// </summary>
  public static UIApplication RevitApp { get; set; }

  public static string Version()
  {
#if  REVIT2020
    return "Revit2020";
#endif
#if  REVIT2023
    return "Revit2023";
#endif
  }
}
