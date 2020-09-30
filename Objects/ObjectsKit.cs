using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Speckle.Objects
{
  public class ObjectsKit : ISpeckleKit
  {

    public string Description => "Default Speckle Kit";
    public string Name => "Objects";
    public string Author => "Speckle";
    public string WebsiteOrEmail => "https://speckle.systems";

    public IEnumerable<Type> Types => Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(Base)) && !t.IsAbstract);

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
          {
            return type.GetInterfaces().FirstOrDefault(iface => iface.Name == typeof(Core.Kits.ISpeckleConverter).Name) != null;
          });

          _LoadedConverters[app] = converterClass;
          return Activator.CreateInstance(converterClass) as ISpeckleConverter;
        }
        else
        {
          throw new SpeckleException($"Converter for {app} was not found in kit {basePath}");
        }

      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        return null;
      }
    }

    public List<string> GetAvailableConverters()
    {
      var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      var list = Directory.EnumerateFiles(basePath, "*.Converter.*");

      return list.ToList().Select(dllPath => dllPath.Split('.').Reverse().ToList()[1]).ToList();
    }
  }
}
