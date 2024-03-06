using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using Microsoft.Extensions.Logging;
using Speckle.Autofac.Files;
using Module = Autofac.Module;

namespace Speckle.Autofac.DependencyInjection;

// POC: wrap the IContainer or expose it?
public class AutofacContainer
{
  private readonly ContainerBuilder _builder;
  private readonly IStorageInfo _storageInfo;

  private IContainer _container;

  public AutofacContainer(IStorageInfo storageInfo)
  {
    _storageInfo = storageInfo;

    _builder = new ContainerBuilder();
  }

  // POC: HOW TO GET TYPES loaded, i.e. kits?
  public AutofacContainer LoadAutofacModules(IEnumerable<string> dependencyPaths)
  {
    // look for assemblies in these paths that offer autofac modules
    foreach (string path in dependencyPaths)
    {
      // find assemblies
      var assembliesInPath = _storageInfo.GetFilenamesInDirectory(path, "*.dll");

      try
      {
        // inspect the assemblies for Autofac.Module
        var assembly = Assembly.ReflectionOnlyLoadFrom(path);
        var moduleClasses = assembly.GetTypes().Where(x => x == typeof(Module));

        //if(moduleClasses.Any())
        //{
        //  // needs loading anyways
        //  // ?
        //}

        // create each module
        foreach (var moduleClass in moduleClasses)
        {
          var module = (Module)Activator.CreateInstance(moduleClass);
          _builder.RegisterModule(module);
        }
      }
      catch (Exception)
      {
        // POC: catch only certain exceptions
        throw;
      }
    }

    return this;
  }

  public AutofacContainer AddModule(Module module)
  {
    _builder.RegisterModule(module);

    return this;
  }

  public AutofacContainer AddSingletonInstance<T>(T instance)
    where T : class
  {
    _builder.RegisterInstance(instance);

    return this;
  }

  public AutofacContainer Build()
  {
    _container = _builder.Build();

    // POC: we could create the factory on construction of the container and then inject that and store it
    var logger = _container.Resolve<ILoggerFactory>().CreateLogger<AutofacContainer>();

    // POC: we could probably expand on this
    List<string> assemblies = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.FullName).ToList();
    logger.LogInformation("Loaded assemblies: {@Assemblies}", assemblies);

    return this;
  }

  public T Resolve<T>()
    where T : class
  {
    // POC: resolve null check with a check and throw perhaps?
    return _container.Resolve<T>();
  }
}
