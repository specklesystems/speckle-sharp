using System.Collections.Generic;

namespace Speckle.Connectors.Revit.Plugin.DllConflictManagment;

public sealed class DllConflictManagmentOptions
{
  public HashSet<string> DllsToIgnore { get; private set; }

  public DllConflictManagmentOptions(HashSet<string> dllsToIgnore)
  {
    DllsToIgnore = dllsToIgnore;
  }
}
