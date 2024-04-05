namespace Speckle.Connectors.Revit.Plugin.DllConflictManagment;

public sealed class DllConflictManagmentOptionsLoader
{
  public DllConflictManagmentOptions LoadOptions()
  {
    return new(true);
  }

  public void SaveOptions(DllConflictManagmentOptions options) { }
}
