using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Speckle.Models;

namespace Speckle.Kits
{
  public interface ISpeckleKit
  {
    /// <summary>
    /// Returns all the object types (the object model) provided by this kit.
    /// </summary>
    IEnumerable<Type> Types { get; }

    /// <summary>
    /// Returns all the converters provided by this kit.
    /// </summary>
    IEnumerable<Type> Converters { get; }

    string Description { get; }
    string Name { get; }
    string Author { get; }
    string WebsiteOrEmail { get; }
  }

  public static class KitManager
  {
    public static readonly string KitsFolder = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "Speckle", "Kits");

    public static readonly AssemblyName SpeckleAssemblyName = typeof(Base).GetTypeInfo().Assembly.GetName();

    private static List<ISpeckleKit> _SpeckleKits = new List<ISpeckleKit>();

    private static List<Type> _AvailableTypes = new List<Type>();

    private static HashSet<string> AssemblyNames = new HashSet<string>();

    private static bool _initialized = false;

    public static IEnumerable<ISpeckleKit> Kits
    {
      get
      {
        if (!_initialized)
        {
          Load();
          _initialized = true;
        }
        return _SpeckleKits;
      }
    }

    public static IEnumerable<Type> Types
    {
      get
      {
        if (!_initialized)
        {
          Load();
          _initialized = true;
        }
        return _AvailableTypes;
      }
    }

    private static void Load()
    {
      GetLoadedSpeckleReferencingAssemblies();
      LoadSpeckleReferencingAssemblies();

      _AvailableTypes = _SpeckleKits.SelectMany(kit => kit.Types).ToList();
    }

    private static void GetLoadedSpeckleReferencingAssemblies()
    {
      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        if (!assembly.IsDynamic && !assembly.ReflectionOnly)
        {
          var kitClass = GetKitClass(assembly);
          if (assembly.IsReferencing(SpeckleAssemblyName) && kitClass != null)
          {
            var isNew = AssemblyNames.Add(assembly.FullName);
            if (isNew)
              _SpeckleKits.Add(Activator.CreateInstance(kitClass) as ISpeckleKit);
          }
        }
      }
    }

    private static void LoadSpeckleReferencingAssemblies()
    {
      var assemblies = new HashSet<Assembly>();
      var directories = Directory.GetDirectories(KitsFolder);
      var currDomain = AppDomain.CurrentDomain;

      foreach (var directory in directories)
      {
        foreach (var assemblyPath in System.IO.Directory.EnumerateFiles(directory, "*.dll"))
        {
          var unloadedAssemblyName = SafeGetAssemblyName(assemblyPath);

          if (unloadedAssemblyName == null)
            continue;

          var assembly = Assembly.LoadFrom(assemblyPath);
          var kitClass = GetKitClass(assembly);
          if (assembly.IsReferencing(SpeckleAssemblyName) && kitClass != null)
          {
            var isNew = AssemblyNames.Add(assembly.FullName);
            if (isNew)
              _SpeckleKits.Add(Activator.CreateInstance(kitClass) as ISpeckleKit);
          }
        }
      }
    }

    private static Type GetKitClass(Assembly assembly)
    {
      var kitClass = assembly.GetTypes().FirstOrDefault(type =>
      {
        return type
        .GetInterfaces()
        .FirstOrDefault(iface =>
        {
          return iface.Name == typeof(Speckle.Kits.ISpeckleKit).Name;
        }) != null;
      });

      return kitClass;
    }

    private static Assembly SafeLoadAssembly(AppDomain domain, AssemblyName assemblyName)
    {
      try
      {
        return domain.Load(assemblyName);
      }
      catch
      {
        return null;
      }
    }

    private static AssemblyName SafeGetAssemblyName(string assemblyPath)
    {
      try
      {
        return AssemblyName.GetAssemblyName(assemblyPath);
      }
      catch
      {
        return null;
      }
    }

  }

  public static class AssemblyExtensions
  {
    /// <summary>
    /// Indicates if a given assembly references another which is identified by its name.
    /// </summary>
    /// <param name="assembly">The assembly which will be probed.</param>
    /// <param name="referenceName">The reference assembly name.</param>
    /// <returns>A boolean value indicating if there is a reference.</returns>
    public static bool IsReferencing(this Assembly assembly, AssemblyName referenceName)
    {
      if (AssemblyName.ReferenceMatchesDefinition(assembly.GetName(), referenceName))
      {
        return true;
      }

      foreach (var referencedAssemblyName in assembly.GetReferencedAssemblies())
      {
        if (AssemblyName.ReferenceMatchesDefinition(referencedAssemblyName, referenceName))
        {
          return true;
        }
      }

      return false;
    }

  }
}
