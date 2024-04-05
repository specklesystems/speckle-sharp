using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Autodesk.Revit.UI;

namespace Speckle.Connectors.Revit.Plugin;

public class DllConflictDetector
{
  private readonly Dictionary<string, AssemblyConflict> _assemblyConflicts = new();

  public void LoadSpeckleAssemblies()
  {
    Dictionary<string, Assembly> loadedAssembliesDict = new();
    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
      // TODO : what to do about multiple versions of loaded dlls?
      loadedAssembliesDict[assembly.GetName().Name] = assembly;
    }
    LoadAssemblyAndDependencies(typeof(DllConflictDetector).Assembly, loadedAssembliesDict, new HashSet<string>());
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
    Trace.WriteLine(assembly.FullName);
    visitedAssemblies.Add(assembly.GetName().Name);

    foreach (var assemblyName in assembly.GetReferencedAssemblies())
    {
      if (visitedAssemblies.Contains(assemblyName.Name))
      {
        continue;
      }

      if (loadedAssemblies.TryGetValue(assemblyName.Name, out Assembly loadedAssembly))
      {
        if (loadedAssembly.GetName().Version != assemblyName.Version)
        {
          _assemblyConflicts[assemblyName.Name] = new(assemblyName, loadedAssembly.GetName(), loadedAssembly.Location);
        }
      }
      else
      {
        loadedAssembly = Assembly.Load(assemblyName);
        loadedAssemblies[assemblyName.Name] = loadedAssembly;
      }

      LoadAssemblyAndDependencies(loadedAssembly, loadedAssemblies, visitedAssemblies);
    }
  }

  public bool HandleTypeLoadException(TypeLoadException ex)
  {
    // getting private fields is a bit naughty..
    var assemblyName = GetPrivateFieldValue<string>(ex, "AssemblyName").Split(',')[0];

    StringBuilder sb = new();
    if (_assemblyConflicts.TryGetValue(assemblyName, out var assemblyConflictInfo))
    {
      sb.AppendLine($"Speckle encountered a dependency mismatch error.");
      sb.AppendLine();
      sb.AppendLine($"Dependency Name: {assemblyName}");
      sb.AppendLine($"Expected Version: {assemblyConflictInfo.SpeckleDependencyAssemblyName.Version}");
      sb.AppendLine($"Actual Version: {assemblyConflictInfo.LoadedDependencyAssemblyName.Version}");
      sb.AppendLine();
      sb.AppendLine($"Conflicting dll folder: {assemblyConflictInfo.GetConflictingExternalAppName()}");

      TaskDialog.Show("Conflict Report 🔥", sb.ToString());
      return true;
    }

    return false;
  }

  private static T GetPrivateFieldValue<T>(object obj, string fieldName)
  {
    if (obj == null)
    {
      throw new ArgumentNullException("obj");
    }

    BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

    var x = obj.GetType().GetFields(bindFlags);

    FieldInfo fi =
      obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
      ?? throw new ArgumentOutOfRangeException(
        "propName",
        string.Format("Property {0} was not found in Type {1}", fieldName, obj.GetType().FullName)
      );

    return (T)fi.GetValue(obj);
  }
}

public class AssemblyConflict
{
  public AssemblyConflict(
    AssemblyName speckleDependencyAssemblyName,
    AssemblyName loadedDependencyAssemblyName,
    string loadedDependencyPath
  )
  {
    SpeckleDependencyAssemblyName = speckleDependencyAssemblyName;
    LoadedDependencyAssemblyName = loadedDependencyAssemblyName;
    LoadedDependencyPath = loadedDependencyPath;
  }

  public AssemblyName SpeckleDependencyAssemblyName { get; set; }
  public AssemblyName LoadedDependencyAssemblyName { get; set; }
  public string LoadedDependencyPath { get; set; }

  public string GetConflictingExternalAppName()
  {
    // example path:
    // "C:\Users\conno\AppData\Roaming\Autodesk\Revit\Addins\2023\Speckle.Connectors.Conflicting.Revit2023\GraphQL.Client.dll"

    var splitByRevitPluginPath = LoadedDependencyPath.Split(
      new[] { "Autodesk\\Revit\\Addins\\" },
      StringSplitOptions.None
    );

    if (
      splitByRevitPluginPath.Length > 1
      && splitByRevitPluginPath[1].Split('\\') is string[] splitByFolder
      && splitByFolder.Length > 1
    )
    {
      return splitByFolder[1];
    }
    else
    {
      return LoadedDependencyPath;
    }
  }
}
