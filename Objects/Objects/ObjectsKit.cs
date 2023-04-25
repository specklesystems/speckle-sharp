#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Speckle.Core.Helpers;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Objects;

/// <summary>
/// The default Speckle Kit
/// </summary>
public class ObjectsKit : ISpeckleKit
{
  private static string? _objectsFolder;

  private readonly Dictionary<string, Type> _loadedConverters = new();

  private List<string>? _converters;

  /// <summary>
  /// Local installations store objects in C:\Users\USERNAME\AppData\Roaming\Speckle\Kits\Objects
  /// Admin/System-wide installations in C:\ProgramData\Speckle\Kits\Objects
  /// </summary>
  public static string ObjectsFolder
  {
    get => _objectsFolder ??= SpecklePathProvider.ObjectsFolderPath;
    [Obsolete("Use " + nameof(SpecklePathProvider.OverrideObjectsFolderName), true)]
    set => _objectsFolder = value;
  }

  /// <inheritdoc/>
  public string Description => "The default Speckle Kit.";

  /// <inheritdoc/>
  public string Name => "Objects";

  /// <inheritdoc/>
  public string Author => "Speckle";

  /// <inheritdoc/>
  public string WebsiteOrEmail => "https://speckle.systems";

  /// <inheritdoc/>
  public IEnumerable<Type> Types =>
    Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(Base)) && !t.IsAbstract);

  /// <inheritdoc/>
  public IEnumerable<string> Converters => _converters ??= GetAvailableConverters();

  /// <inheritdoc/>
  public ISpeckleConverter LoadConverter(string app)
  {
    try
    {
      _converters = GetAvailableConverters();
      if (_loadedConverters.TryGetValue(app, out Type t))
        return (ISpeckleConverter)Activator.CreateInstance(t);

      var converterInstance = LoadConverterFromDisk(app);
      _loadedConverters[app] = converterInstance.GetType();

      return converterInstance;
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Warning(ex, "Failed to load converter for app {app}", app);
      throw new KitException($"Failed to load converter for app {app}", this, ex);
    }
  }

  private static ISpeckleConverter LoadConverterFromDisk(string app)
  {
    var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    var path = Path.Combine(basePath!, $"Objects.Converter.{app}.dll");

    //fallback to the default folder, in case the Objects.dll was loaded in the app domain for other reasons
    if (!File.Exists(path))
      path = Path.Combine(ObjectsFolder, $"Objects.Converter.{app}.dll");

    if (!File.Exists(path))
      throw new FileNotFoundException($"Converter for {app} was not found in kit {basePath}", path);

    var assembly = Assembly.LoadFrom(path);

    var converterInstance = assembly
      .GetTypes()
      .Where(type => typeof(ISpeckleConverter).IsAssignableFrom(type))
      .Select(type => (ISpeckleConverter)Activator.CreateInstance(type))
      .First(converter => converter.GetServicedApplications().Contains(app));

    SpeckleLog.Logger
      .ForContext<ObjectsKit>()
      .ForContext("basePath", basePath)
      .ForContext("app", app)
      .Information("Converter {converterName} successfully loaded from {path}", converterInstance.Name, path);

    return converterInstance;
  }

  public List<string> GetAvailableConverters()
  {
    var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    var availableConverters = Directory
      .EnumerateFiles(basePath!, "Objects.Converter.*")
      .Select(dllPath => dllPath.Split('.').Reverse().ElementAt(1))
      .ToList();

    //fallback to the default folder, in case the Objects.dll was loaded in the app domain for other reasons
    if (!availableConverters.Any())
      availableConverters = Directory
        .EnumerateFiles(ObjectsFolder, "Objects.Converter.*")
        .Select(dllPath => dllPath.Split('.').Reverse().ElementAt(1))
        .ToList();

    return availableConverters;
  }
}
