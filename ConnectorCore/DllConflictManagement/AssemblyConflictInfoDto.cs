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
    ConflictingAssemblyName = conflictingAssembly.Name;
    ConflictingAssemblyVersion = conflictingAssembly.Version.ToString();
    SpeckleExpectedVersion = speckleDependencyAssemblyName.Version.ToString();
    FolderName = folderName;
    Path = fullPath;
  }

  public string SpeckleExpectedVersion { get; }
  public string ConflictingAssemblyName { get; }
  public string ConflictingAssemblyVersion { get; }
  public string FolderName { get; }
  public string Path { get; }
}

public static class AssemblyConflictInfoExtensions
{
  public static AssemblyConflictInfoDto ToDto(this AssemblyConflictInfo assemblyConflictInfo)
  {
    return new(
      assemblyConflictInfo.SpeckleDependencyAssemblyName,
      assemblyConflictInfo.ConflictingAssembly.GetName(),
      assemblyConflictInfo.GetConflictingExternalAppName(),
      GetPathFromAutodeskOrFullPath(assemblyConflictInfo.ConflictingAssembly.Location)
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

  private static readonly string[] s_separator = new[] { "Autodesk" };

  private static string GetPathFromAutodeskOrFullPath(string fullPath)
  {
    string[] splitOnAutodesk = fullPath.Split(s_separator, StringSplitOptions.None);

    if (splitOnAutodesk.Length == 2)
    {
      return splitOnAutodesk[1];
    }
    return fullPath;
  }
}
