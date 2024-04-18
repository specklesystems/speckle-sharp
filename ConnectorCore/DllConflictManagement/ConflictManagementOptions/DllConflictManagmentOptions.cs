namespace Speckle.DllConflictManagement.ConflictManagementOptions;

public sealed class DllConflictManagmentOptions
{
  public HashSet<string> DllsToIgnore { get; private set; }

  public DllConflictManagmentOptions(HashSet<string> dllsToIgnore)
  {
    DllsToIgnore = dllsToIgnore;
  }
}
