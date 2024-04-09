using System.Reflection;

namespace DllConflictManagment;

public sealed class DllConflictManager
{
  private readonly Dictionary<string, AssemblyConflictInfo> _assemblyConflicts = new();
  private readonly DllConflictManagmentOptionsLoader _optionsLoader;
  private readonly string[] _assemblyPathFragmentsToIgnore;
  public IEnumerable<AssemblyConflictInfo> AllConflictInfo => _assemblyConflicts.Values;

  public DllConflictManager(
    DllConflictManagmentOptionsLoader optionsLoader,
    params string[] assemblyPathFragmentsToIgnore
  )
  {
    _optionsLoader = optionsLoader;
    _assemblyPathFragmentsToIgnore = assemblyPathFragmentsToIgnore;
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
        bool shouldIgnore = false;
        foreach (var pathFragment in _assemblyPathFragmentsToIgnore)
        {
          if (loadedAssembly.Location.Contains(pathFragment))
          {
            shouldIgnore = true;
            break;
          }
        }
        if (loadedAssembly.GetName().Version != assemblyName.Version && !shouldIgnore)
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

  private static Assembly? GetLoadedAssembly(AssemblyName assemblyName)
  {
    try
    {
      return Assembly.Load(assemblyName);
    }
    catch (FileNotFoundException)
    {
      // how do we add logging when the logger may be part of a dll conflict?
    }
    catch (FileLoadException)
    {
      // POC : add logging
    }
    catch (BadImageFormatException)
    {
      // POC : add logging
    }
    return null;
  }

  public AssemblyConflictInfo? GetConflictThatCausedException(MissingMethodException ex)
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
      throw new ArgumentNullException("obj");
    }

    FieldInfo fi =
      obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
      ?? throw new ArgumentOutOfRangeException(
        "propName",
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
    return _assemblyConflicts.Values.Where(
      info => !options.DllsToIgnore.Contains(info.SpeckleDependencyAssemblyName.Name)
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
