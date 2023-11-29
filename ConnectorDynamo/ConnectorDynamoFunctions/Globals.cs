using Autodesk.DesignScript.Runtime;

namespace Speckle.ConnectorDynamo.Functions;

[IsVisibleInDynamoLibrary(false)]
public static class Globals
{
  /// <summary>
  /// Cached Revit Document, required to properly scale incoming / outcoming geometry
  /// </summary>
  public static object RevitDocument { get; set; }
}
