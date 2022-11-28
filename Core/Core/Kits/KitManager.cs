using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Speckle.Core.Api;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.Core.Kits
{

  public static class KitManager
  {

    private static string _kitsFolder = null;
    /// <summary>
    /// Local installations store kits in C:\Users\USERNAME\AppData\Roaming\Speckle\Kits
    /// Admin/System-wide installations in C:\ProgramData\Speckle\Kits
    /// </summary>
    public static string KitsFolder
    {
      get
      {
        if (_kitsFolder == null)
          _kitsFolder = Path.Combine(Helpers.InstallSpeckleFolderPath, "Kits");

        return _kitsFolder;
      }
      set
      {
        _kitsFolder = value;
      }
    }

    public static readonly AssemblyName SpeckleAssemblyName = typeof(Base).GetTypeInfo().Assembly.GetName();

    private static Dictionary<string, ISpeckleKit> _SpeckleKits = new Dictionary<string, ISpeckleKit>();

    private static List<Type> _AvailableTypes = new List<Type>();

    private static bool _initialized = false;

    /// <summary>
    /// Checks wether a specific kit exists.
    /// </summary>
    /// <param name="assemblyFullName"></param>
    /// <returns></returns>
    public static bool HasKit(string assemblyFullName)
    {
      Initialize();
      return _SpeckleKits.ContainsKey(assemblyFullName);
    }

    /// <summary>
    /// Gets a specific kit.
    /// </summary>
    /// <param name="assemblyFullName"></param>
    /// <returns></returns>
    public static ISpeckleKit GetKit(string assemblyFullName)
    {
      Initialize();
      return _SpeckleKits[assemblyFullName];
    }

    /// <summary>
    /// Returns a list of all the kits found on this user's device.
    /// </summary>
    public static IEnumerable<ISpeckleKit> Kits
    {
      get
      {
        Initialize();
        return _SpeckleKits.Values.Where(v => v != null);
      }
    }

    /// <summary>
    /// Returns a list of all the types found in all the kits on this user's device.
    /// </summary>
    public static IEnumerable<Type> Types
    {
      get
      {
        Initialize();
        return _AvailableTypes;
      }
    }

    /// <summary>
    /// Gets the default Speckle provided kit, "Objects".
    /// </summary>
    /// <returns></returns>
    public static ISpeckleKit GetDefaultKit()
    {
      Initialize();
      return _SpeckleKits.FirstOrDefault(kvp => kvp.Value.Name == "Objects").Value;
    }

    /// <summary>
    /// TODO: Returns all the kits with potential converters for the software app. 
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IEnumerable<ISpeckleKit> GetKitsWithConvertersForApp(string app)
    {
      foreach (var kit in Kits)
      {
        if (kit.Converters.Contains(app))
          yield return kit;
      }
    }

    /// <summary>
    /// Tells the kit manager to initialise from a specific location.
    /// </summary>
    /// <param name="kitFolderLocation"></param>
    public static void Initialize(string kitFolderLocation)
    {
      if (_initialized) throw new SpeckleException("The kit manager has already been initialised. Make sure you call this method earlier in your code!", level: Sentry.SentryLevel.Warning);

      KitsFolder = kitFolderLocation;
      Load();
      _initialized = true;
    }

    #region Private Methods

    private static void Initialize()
    {
      if (!_initialized)
      {
        Load();
        _initialized = true;
      }
    }

    private static void Load()
    {
      Log.AddBreadcrumb("Initialize Kit Manager");

      GetLoadedSpeckleReferencingAssemblies();
      LoadSpeckleReferencingAssemblies();

      _AvailableTypes = _SpeckleKits
        .Where(kit => kit.Value != null)
        .SelectMany(kit => kit.Value.Types).ToList();
    }

    // recursive search for referenced assemblies
    public static List<Assembly> GetReferencedAssemblies()
    {
      var returnAssemblies = new List<Assembly>();
      var loadedAssemblies = new HashSet<string>();
      var assembliesToCheck = new Queue<Assembly>();

      assembliesToCheck.Enqueue(Assembly.GetEntryAssembly());

      while (assembliesToCheck.Count > 0)
      {
        var assemblyToCheck = assembliesToCheck.Dequeue();

        if (assemblyToCheck == null)
          continue;

        foreach (var reference in assemblyToCheck.GetReferencedAssemblies())
        {
          // filtering out system dlls
          if (reference.FullName.StartsWith("System.") || reference.FullName.StartsWith("Microsoft."))
            continue;

          if (!loadedAssemblies.Contains(reference.FullName))
          {
            Assembly assembly = null;
            try
            {
              assembly = Assembly.Load(reference);
            }
            catch
            {
              continue;
            }
            assembliesToCheck.Enqueue(assembly);
            loadedAssemblies.Add(reference.FullName);
            returnAssemblies.Add(assembly);
          }
        }
      }

      return returnAssemblies;
    }

    private static void GetLoadedSpeckleReferencingAssemblies()
    {
      List<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
      assemblies.AddRange(GetReferencedAssemblies());

      foreach (var assembly in assemblies)
      {
        if (!assembly.IsDynamic && !assembly.ReflectionOnly)
        {
          var kitClass = GetKitClass(assembly);
          if (assembly.IsReferencing(SpeckleAssemblyName) && kitClass != null)
          {
            if (!_SpeckleKits.ContainsKey(assembly.FullName))
            {
              var speckleKit = Activator.CreateInstance(kitClass) as ISpeckleKit;
              if (speckleKit != null) _SpeckleKits.Add(assembly.FullName, speckleKit);
            }
          }
        }
      }
    }

    private static void LoadSpeckleReferencingAssemblies()
    {
      if (!Directory.Exists(KitsFolder))
        return;

      var assemblies = new HashSet<Assembly>();
      var directories = Directory.GetDirectories(KitsFolder);
      var currDomain = AppDomain.CurrentDomain;

      foreach (var directory in directories)
      {
        foreach (var assemblyPath in Directory.EnumerateFiles(directory, "*.dll"))
        {
          var unloadedAssemblyName = SafeGetAssemblyName(assemblyPath);

          if (unloadedAssemblyName == null)
            continue;

          try
          {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var kitClass = GetKitClass(assembly);
            if (assembly.IsReferencing(SpeckleAssemblyName) && kitClass != null)
            {
              if (!_SpeckleKits.ContainsKey(assembly.FullName))
              {
                var speckleKit = Activator.CreateInstance(kitClass) as ISpeckleKit;
                if (speckleKit != null) _SpeckleKits.Add(assembly.FullName, speckleKit);
              }
            }
          }
          catch (FileLoadException ex)
          {
          }
        }
      }
    }

    private static Type GetKitClass(Assembly assembly)
    {
      try
      {
        var kitClass = assembly.GetTypes().FirstOrDefault(type =>
        {
          return type
            .GetInterfaces()
            .FirstOrDefault(iface =>
            {
              return iface.Name == typeof(Speckle.Core.Kits.ISpeckleKit).Name;
            }) != null;
        });

        return kitClass;
      }
      catch
      {
        // this will be a ReflectionTypeLoadException and is expected. we don't need to care!
        return null;
      }
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

    #endregion
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
