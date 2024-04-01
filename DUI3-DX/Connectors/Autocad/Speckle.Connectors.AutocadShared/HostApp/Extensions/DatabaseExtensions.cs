using Autodesk.AutoCAD.DatabaseServices;

namespace Speckle.Connectors.Autocad.HostApp.Extensions;

public static class DatabaseExtensions
{
  /// <summary>
  /// Gets the document model space
  /// </summary>
  /// <param name="db"></param>
  /// <param name="mode"></param>
  /// <returns></returns>
  public static BlockTableRecord GetModelSpace(this Database db, OpenMode mode = OpenMode.ForRead)
  {
    return (BlockTableRecord)SymbolUtilityServices.GetBlockModelSpaceId(db).GetObject(mode);
  }
}
