using System.Reflection;

namespace Speckle.DllConflictManagement;

/// <summary>
/// Some system object types (System.Type, System.Reflection.AssemblyName, System.Reflection.Assembly) were throwing
/// when I was attempting to serialize the <see cref="AssemblyConflictInfo"/> objects to be sent to mixpanel.
/// Therefore I made this DTO to make an object that would hold the info we need and would be serializable.
/// </summary>
public sealed class AssemblyConflictInfoDto
{
  public AssemblyConflictInfoDto(
    AssemblyName speckleDependencyAssemblyName,
    AssemblyName conflictingAssembly,
    string folderName,
    string fullPath
  )
  {
    SpeckleDependencyAssemblyName = speckleDependencyAssemblyName.FullName;
    SpeckleDependencyAssemblyVersion = speckleDependencyAssemblyName.Version;
    ConflictingAssemblyName = conflictingAssembly.FullName;
    ConflictingAssemblyVersion = conflictingAssembly.Version;
    FolderName = folderName;
    FullPath = fullPath;
  }

  public string SpeckleDependencyAssemblyName { get; }
  public Version SpeckleDependencyAssemblyVersion { get; }
  public string ConflictingAssemblyName { get; }
  public Version ConflictingAssemblyVersion { get; }
  public string FolderName { get; }
  public string FullPath { get; }
}

public static class AssemblyConflictInfoExtensions
{
  public static AssemblyConflictInfoDto ToDto(this AssemblyConflictInfo assemblyConflictInfo)
  {
    return new(
      assemblyConflictInfo.SpeckleDependencyAssemblyName,
      assemblyConflictInfo.ConflictingAssembly.GetName(),
      assemblyConflictInfo.GetConflictingExternalAppName(),
      assemblyConflictInfo.ConflictingAssembly.Location
    );
  }

  public static IEnumerable<AssemblyConflictInfoDto> ToDtos(
    this IEnumerable<AssemblyConflictInfo> assemblyConflictInfos
  )
  {
    foreach (var assemblyConflictInfo in assemblyConflictInfos)
    {
      yield return assemblyConflictInfo.ToDto();
    }
  }
}
