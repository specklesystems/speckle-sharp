using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using Microsoft.Extensions.Logging;
using Speckle.Autofac.Files;
using Speckle.Core.Logging;
using Module = Autofac.Module;

namespace Speckle.Autofac.DependencyInjection;

// POC: wrap the IContainer or expose it?
public class AutofacContainer
{
  public delegate void PreBuildEventHandler(object sender, ContainerBuilder containerBuilder);

  // Declare the event.
  public event PreBuildEventHandler PreBuildEvent;

  private readonly ContainerBuilder _builder;
  private readonly IStorageInfo _storageInfo;

  private IContainer _container;

  public AutofacContainer(IStorageInfo storageInfo)
  {
    _storageInfo = storageInfo;

    _builder = new ContainerBuilder();
  }

  // POC: HOW TO GET TYPES loaded, this feels a bit heavy handed and relies on Autofac where we can probably do something different
  public AutofacContainer LoadAutofacModules(IEnumerable<string> dependencyPaths)
  {
    // look for assemblies in these paths that offer autofac modules
    foreach (string path in dependencyPaths)
    {
      // POC: naming conventions
      // find assemblies
      var assembliesInPath = _storageInfo.GetFilenamesInDirectory(path, "Speckle*.dll");

      foreach (var file in assembliesInPath)
      {
        // POC: ignore already loaded? Or just get that instead of loading it?

        try
        {
          // inspect the assemblies for Autofac.Module
          var assembly = Assembly.LoadFrom(file);
          var moduleClasses = assembly.GetTypes().Where(x => x.BaseType == typeof(Module));

          // create each module
          // POC: could look for some attribute here
          foreach (var moduleClass in moduleClasses)
          {
            var module = (Module)Activator.CreateInstance(moduleClass);
            _builder.RegisterModule(module);
          }
        }
        // POC: catch only certain exceptions
        catch (Exception ex) when (!ex.IsFatal()) { }
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
    // before we build give some last minute registration options.
    PreBuildEvent?.Invoke(this, _builder);

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
