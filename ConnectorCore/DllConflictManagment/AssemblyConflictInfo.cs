using System.Reflection;

namespace DllConflictManagment;

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
}
