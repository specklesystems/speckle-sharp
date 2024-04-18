using System.Reflection;
using System.Text;

namespace Speckle.DllConflictManagement;

public sealed class AssemblyConflictInfo
{
  public AssemblyConflictInfo(AssemblyName speckleDependencyAssemblyName, Assembly conflictingAssembly)
  {
    SpeckleDependencyAssemblyName = speckleDependencyAssemblyName;
    ConflictingAssembly = conflictingAssembly;
  }

  public AssemblyName SpeckleDependencyAssemblyName { get; set; }
  public Assembly ConflictingAssembly { get; set; }

  public string GetConflictingExternalAppName() =>
    new DirectoryInfo(Path.GetDirectoryName(ConflictingAssembly.Location)).Name;

  public override string ToString()
  {
    StringBuilder sb = new();
    sb.AppendLine($"Conflict DLL: {SpeckleDependencyAssemblyName.Name}");
    sb.AppendLine($"SpeckleVer: {SpeckleDependencyAssemblyName.Version}");
    sb.AppendLine($"LoadedVer: {ConflictingAssembly.GetName().Version}");
    sb.AppendLine($"Folder: {GetConflictingExternalAppName()}");
    return sb.ToString();
  }
}
