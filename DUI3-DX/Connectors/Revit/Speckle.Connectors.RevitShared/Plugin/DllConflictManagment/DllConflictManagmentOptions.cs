namespace Speckle.Connectors.Revit.Plugin.DllConflictManagment;

public sealed class DllConflictManagmentOptions
{
  public bool ShowConflictWarning { get; set; }

  public DllConflictManagmentOptions(bool showConflictWarning)
  {
    ShowConflictWarning = showConflictWarning;
  }
}
