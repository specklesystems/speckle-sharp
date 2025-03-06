using System.Reflection;
using Speckle.DllConflictManagement.ConflictManagementOptions;
using Speckle.DllConflictManagement.EventEmitter;

namespace Speckle.DllConflictManagement;

public sealed class DllConflictManager
{
  private readonly Dictionary<string, AssemblyConflictInfo> _assemblyConflicts = new();
  private readonly DllConflictManagmentOptionsLoader _optionsLoader;
  private readonly DllConflictEventEmitter _eventEmitter;
  private readonly string[] _assemblyPathFragmentsToIgnore;
  private readonly string[] _exactAssemblyPathsToIgnore;

  public ICollection<AssemblyConflictInfo> AllConflictInfo => _assemblyConflicts.Values;
  public ICollection<AssemblyConflictInfoDto> AllConflictInfoAsDtos => _assemblyConflicts.Values.ToDtos().ToList();

  public DllConflictManager(
    DllConflictManagmentOptionsLoader optionsLoader,
    DllConflictEventEmitter eventEmitter,
    string[]? assemblyPathFragmentsToIgnore = null,
    string[]? exactAssemblyPathsToIgnore = null
  )
  {
    _optionsLoader = optionsLoader;
    _eventEmitter = eventEmitter;
    _assemblyPathFragmentsToIgnore = assemblyPathFragmentsToIgnore ?? Array.Empty<string>();
    _exactAssemblyPathsToIgnore = exactAssemblyPathsToIgnore ?? Array.Empty<string>();
  }

  /// <summary>
  /// Detects dll conflicts (same dll name with different strong version) between the assemblies loaded into
  /// the current domain, and the assemblies that are dependencies of the provided assembly.
  /// The dependency conflicts are stored in the conflictManager to be retreived later.
  /// </summary>
  /// <param name="providedAssembly"></param>
  public void DetectConflictsWithAssembliesInCurrentDomain(Assembly providedAssembly)
  {
    Dictionary<string, Assembly> loadedAssembliesDict = new();
    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
      loadedAssembliesDict[assembly.GetName().Name] = assembly;
    }
    LoadAssemblyAndDependencies(providedAssembly, loadedAssembliesDict, new HashSet<string>());
  }

  private void LoadAssemblyAndDependencies(
    Assembly assembly,
    Dictionary<string, Assembly> loadedAssemblies,
    HashSet<string> visitedAssemblies
  )
  {
    if (visitedAssemblies.Contains(assembly.GetName().Name))
    {
      return;
    }
    visitedAssemblies.Add(assembly.GetName().Name);

    foreach (var assemblyName in assembly.GetReferencedAssemblies())
    {
      if (visitedAssemblies.Contains(assemblyName.Name))
      {
        continue;
      }

      if (loadedAssemblies.TryGetValue(assemblyName.Name, out Assembly? loadedAssembly))
      {
        bool shouldSkip = ShouldSkipCheckingConflictBecauseOfAssemblyLocation(loadedAssembly);
        if (!shouldSkip && !MajorAndMinorVersionsEqual(loadedAssembly.GetName().Version, assemblyName.Version))
        {
          _assemblyConflicts[assemblyName.Name] = new(assemblyName, loadedAssembly);
          continue; // if we already know there is a conflict here, no need to continue iterating dependencies
        }
      }
      else
      {
        loadedAssembly = GetLoadedAssembly(assemblyName);
        if (loadedAssembly is not null)
        {
          loadedAssemblies[assemblyName.Name] = loadedAssembly;
        }
      }

      if (loadedAssembly is not null)
      {
        LoadAssemblyAndDependencies(loadedAssembly, loadedAssemblies, visitedAssemblies);
      }
    }
  }

  private static bool PathIsValid(string path)
  {
    if (path.Any(x => Path.GetInvalidPathChars().Contains(x)))
    {
      return false;
    }
    return true;
  }

  private bool ShouldSkipCheckingConflictBecauseOfAssemblyLocation(Assembly? loadedAssembly)
  {
    if (string.IsNullOrWhiteSpace(loadedAssembly?.Location))
    {
      return false;
    }
    string location = loadedAssembly!.Location;
    if (!PathIsValid(location))
    {
      return false;
    }
    foreach (var exactPath in _exactAssemblyPathsToIgnore)
    {
      if (Path.GetDirectoryName(location) == exactPath)
      {
        return true;
      }
    }

    foreach (var pathFragment in _assemblyPathFragmentsToIgnore)
    {
      if (location.Contains(pathFragment))
      {
        return true;
      }
    }

    return false;
  }

  private static bool MajorAndMinorVersionsEqual(Version version1, Version version2)
  {
    return version1.Major == version2.Major && version1.Minor == version2.Minor;
  }

  private Assembly? GetLoadedAssembly(AssemblyName assemblyName)
  {
    try
    {
      return Assembly.Load(assemblyName);
    }
    catch (FileNotFoundException)
    {
      // trying to load Objects.dll will result in this exception, but that is okay because its only dependency
      // is core which will be checked as a dependency of many other libraries.
      // there are a couple other random types that will trigger this exception as well
    }
    catch (FileLoadException)
    {
      // this is new for .NET Core and is invalid for Speckle assemblies.  Not catching causes other issues
      // with other plugins so swallowing is the best we can do.
    }
    return null;
  }

  public AssemblyConflictInfo? GetConflictThatCausedException(MemberAccessException ex)
  {
    if (
      TryParseTypeNameFromMissingMethodExceptionMessage(ex.Message, out var typeName)
      && TryGetTypeFromName(typeName!, out var type)
      && _assemblyConflicts.TryGetValue(type!.Assembly.GetName().Name, out var assemblyConflictInfo)
    )
    {
      return assemblyConflictInfo;
    }
    return null;
  }

  private static bool TryParseTypeNameFromMissingMethodExceptionMessage(string message, out string? typeName)
  {
    typeName = null;

    var splitOnApostraphe = message.Split('\'');
    if (splitOnApostraphe.Length < 2)
    {
      return false;
    }

    var splitOnSpace = splitOnApostraphe[1].Split(' ');
    if (splitOnSpace.Length < 2)
    {
      return false;
    }

    var splitOnPeriod = splitOnSpace[1].Split('.');
    if (splitOnPeriod.Length < 3)
    {
      return false;
    }

    typeName = string.Join(".", splitOnPeriod.Take(splitOnPeriod.Length - 1));
    return true;
  }

  private static bool TryGetTypeFromName(string typeName, out Type? type)
  {
    type = null;
    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Reverse())
    {
      if (assembly.GetType(typeName) is Type foundType)
      {
        type = foundType;
        return true;
      }
    }
    return false;
  }

  public AssemblyConflictInfo? GetConflictThatCausedException(TypeLoadException ex)
  {
    // getting private fields is a bit naughty..
    var assemblyName = GetPrivateFieldValue<string?>(ex, "AssemblyName")?.Split(',').FirstOrDefault();

    if (assemblyName != null && _assemblyConflicts.TryGetValue(assemblyName, out var assemblyConflictInfo))
    {
      return assemblyConflictInfo;
    }
    return null;
  }

  private static T GetPrivateFieldValue<T>(object obj, string fieldName)
  {
    if (obj == null)
    {
      throw new ArgumentNullException(nameof(obj));
    }

    FieldInfo fi =
      obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
      ?? throw new ArgumentOutOfRangeException(
        nameof(fieldName),
        string.Format("Property {0} was not found in Type {1}", fieldName, obj.GetType().FullName)
      );

    return (T)fi.GetValue(obj);
  }

  public IEnumerable<AssemblyConflictInfo> GetConflictInfoThatUserHasntSuppressedWarningsFor()
  {
    if (_assemblyConflicts.Count == 0)
    {
      return Enumerable.Empty<AssemblyConflictInfo>();
    }

    var options = _optionsLoader.LoadOptions();
    return _assemblyConflicts.Values.Where(info =>
      !options.DllsToIgnore.Contains(info.SpeckleDependencyAssemblyName.Name)
    );
  }

  public void SupressFutureAssemblyConflictWarnings(IEnumerable<AssemblyConflictInfo> conflictsToIgnoreWarningsFor)
  {
    var options = _optionsLoader.LoadOptions();
    foreach (var conflictInfo in conflictsToIgnoreWarningsFor)
    {
      options.DllsToIgnore.Add(conflictInfo.SpeckleDependencyAssemblyName.Name);
    }
    _optionsLoader.SaveOptions(options);
  }
}
