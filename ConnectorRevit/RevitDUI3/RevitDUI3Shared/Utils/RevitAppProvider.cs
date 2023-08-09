using Autodesk.Revit.UI;

namespace Speckle.ConnectorRevitDUI3.Utils;

public static class RevitAppProvider
{
  /// <summary>
  /// This property gets initialized in the plugin entry, on app initialization.
  /// We should be able to use this in any place where we need the revit app! 
  /// </summary>
  public static UIApplication RevitApp { get; set; }
}
