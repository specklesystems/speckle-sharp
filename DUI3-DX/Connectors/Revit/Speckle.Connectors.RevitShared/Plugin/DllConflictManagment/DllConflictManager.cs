using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Autodesk.Revit.UI;

namespace Speckle.Connectors.Revit.Plugin.DllConflictManagment;

public class DllConflictManager
{
  private readonly Dictionary<string, AssemblyConflictInfo> _assemblyConflicts = new();
  private readonly DllConflictManagmentOptionsLoader _optionsLoader;

  public DllConflictManager(DllConflictManagmentOptionsLoader optionsLoader)
  {
    _optionsLoader = optionsLoader;
  }

  public void DetectConflicts()
  {
    Dictionary<string, Assembly> loadedAssembliesDict = new();
    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
      // TODO : what to do about multiple versions of loaded dlls?
      loadedAssembliesDict[assembly.GetName().Name] = assembly;
    }
    LoadAssemblyAndDependencies(typeof(DllConflictManager).Assembly, loadedAssembliesDict, new HashSet<string>());
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
        if (loadedAssembly.GetName().Version != assemblyName.Version)
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
      // POC : add logging
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

  public bool HandleTypeMissingMethodException(MissingMethodException ex)
  {
    StringBuilder sb = new();
    if (
      TryParseTypeNameFromMissingMethodExceptionMessage(ex.Message, out var typeName)
      && TryGetTypeFromName(typeName!, out var type)
      && _assemblyConflicts.TryGetValue(type!.Assembly.GetName().Name, out var assemblyConflictInfo)
    )
    {
      sb.AppendLine($"Speckle encountered a dependency mismatch error.");
      sb.AppendLine();
      sb.AppendLine($"Dependency Name: {type!.Assembly.GetName().Name}");
      sb.AppendLine($"Expected Version: {assemblyConflictInfo.SpeckleDependencyAssemblyName.Version}");
      sb.AppendLine($"Actual Version: {assemblyConflictInfo.ConflictingAssembly.GetName().Version}");
      sb.AppendLine();
      sb.AppendLine($"Conflicting dll folder: {assemblyConflictInfo.GetConflictingExternalAppName()}");

      TaskDialog.Show("Conflict Report 🔥", sb.ToString());
      return true;
    }
    return false;
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

  public bool HandleTypeLoadException(TypeLoadException ex)
  {
    // getting private fields is a bit naughty..
    var assemblyName = GetPrivateFieldValue<string?>(ex, "AssemblyName")?.Split(',').FirstOrDefault();

    StringBuilder sb = new();
    if (assemblyName != null && _assemblyConflicts.TryGetValue(assemblyName, out var assemblyConflictInfo))
    {
      sb.AppendLine($"Speckle encountered a dependency mismatch error.");
      sb.AppendLine();
      sb.AppendLine($"Dependency Name: {assemblyName}");
      sb.AppendLine($"Expected Version: {assemblyConflictInfo.SpeckleDependencyAssemblyName.Version}");
      sb.AppendLine($"Actual Version: {assemblyConflictInfo.ConflictingAssembly.GetName().Version}");
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

    FieldInfo fi =
      obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
      ?? throw new ArgumentOutOfRangeException(
        "propName",
        string.Format("Property {0} was not found in Type {1}", fieldName, obj.GetType().FullName)
      );

    return (T)fi.GetValue(obj);
  }

  public void WarnUserOfPossibleConflicts()
  {
    if (_assemblyConflicts.Count == 0)
    {
      return;
    }

    var options = _optionsLoader.LoadOptions();
    List<AssemblyConflictInfo> conflictInfoNotIgnored = _assemblyConflicts.Values
      .Where(info => !options.DllsToIgnore.Contains(info.SpeckleDependencyAssemblyName.Name))
      .ToList();

    if (conflictInfoNotIgnored.Count == 0)
    {
      return;
    }

    StringBuilder sb = new();
    foreach (var assemblyConflictInfo in conflictInfoNotIgnored)
    {
      sb.AppendLine($"Dependency Name:  {assemblyConflictInfo.SpeckleDependencyAssemblyName.Name}");
      sb.AppendLine($"Expected Version: {assemblyConflictInfo.SpeckleDependencyAssemblyName.Version}");
      sb.AppendLine($"Actual Version:   {assemblyConflictInfo.ConflictingAssembly.GetName().Version}");
      sb.AppendLine($"Conflicting version folder: {assemblyConflictInfo.GetConflictingExternalAppName()}");
      sb.AppendLine();
    }

    using var dialog = new TaskDialog("Conflict Report 🔥");
    dialog.MainInstruction =
      "Speckle encountered an error loading the following dependencies. This is likely due to a different addin that uses a different version of the same dependency.";

    dialog.ExtraCheckBoxText = "Do not warn about this conflict again";
    dialog.MainContent = sb.ToString();
    dialog.CommonButtons = TaskDialogCommonButtons.Ok;

    _ = dialog.Show();

    if (dialog.WasExtraCheckBoxChecked())
    {
      foreach (var conflictInfo in conflictInfoNotIgnored)
      {
        options.DllsToIgnore.Add(conflictInfo.SpeckleDependencyAssemblyName.Name);
      }
      _optionsLoader.SaveOptions(options);
    }
  }
}
