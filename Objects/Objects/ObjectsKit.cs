
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Objects
{
  /// <summary>
  /// The default Speckle Kit
  /// </summary>
  public class ObjectsKit : ISpeckleKit
  {
    /// <inheritdoc/>
    public string Description => "The default Speckle Kit.";
    
    /// <inheritdoc/>
    public string Name => "Objects";
    
    /// <inheritdoc/>
    public string Author => "Speckle";
    
    /// <inheritdoc/>
    public string WebsiteOrEmail => "https://speckle.systems";

    /// <inheritdoc/>
    public IEnumerable<Type> Types
    {
      get
      {
        //the types in this assembly
        var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(Base)) && !t.IsAbstract);
        //try
        //{
        //  //the types that are in a separate assembly, eg Objects.Revit.dll
        //  var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        //  var list = Directory.EnumerateFiles(basePath, "Objects.*.dll").Where(x => !x.Contains("Converter")); //TODO: replace with regex

        //  foreach (var path in list)
        //  {
        //    var assembly = Assembly.LoadFrom(path);
        //    types = types.Concat(assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Base)) && !t.IsAbstract));
        //  }
        //}
        //catch { }

        return types;
      }
    }

    public List<string> _Converters;
    public IEnumerable<string> Converters
    {
      get
      {
        if (_Converters == null)
        {
          _Converters = GetAvailableConverters();
        }

        return _Converters;
      }
    }

    private Dictionary<string, Type> _LoadedConverters = new Dictionary<string, Type>();

    public ISpeckleConverter LoadConverter(string app)
    {
      _Converters = GetAvailableConverters();
      if (_LoadedConverters.ContainsKey(app) && _LoadedConverters[app] != null)
      {
        return Activator.CreateInstance(_LoadedConverters[app]) as ISpeckleConverter;
      }

      try
      {
        var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        var path = Path.Combine(basePath, $"Objects.Converter.{app}.dll");
        if (File.Exists(path))
        {
          var assembly = Assembly.LoadFrom(path);

          var converterClass = assembly.GetTypes().FirstOrDefault(type =>
            (type.GetInterfaces().FirstOrDefault(i => i.Name == typeof(ISpeckleConverter).Name) != null) &&
             (Activator.CreateInstance(type) as ISpeckleConverter).GetServicedApplications().Contains(app)
          );

          _LoadedConverters[app] = converterClass;
          return Activator.CreateInstance(converterClass) as ISpeckleConverter;
        }
        else
        {
          throw new SpeckleException($"Converter for {app} was not found in kit {basePath}", level: Sentry.SentryLevel.Warning);
        }

      }
      catch (Exception e)
      {
        Log.CaptureException(e, Sentry.SentryLevel.Error);
        return null;
      }
    }

    public List<string> GetAvailableConverters()
    {
      var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      var list = Directory.EnumerateFiles(basePath, "Objects.Converter.*");

      return list.ToList().Select(dllPath => dllPath.Split('.').Reverse().ToList()[1]).ToList();
    }
  }
}
