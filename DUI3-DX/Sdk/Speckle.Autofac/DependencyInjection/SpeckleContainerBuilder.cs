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

public class SpeckleContainerBuilder
{
  private readonly IStorageInfo _storageInfo;

  private SpeckleContainerBuilder(IStorageInfo storageInfo)
  {
    _storageInfo = storageInfo;
    ContainerBuilder = new ContainerBuilder();
  }

  public static SpeckleContainerBuilder CreateInstance() => new(new StorageInfo());

  // POC: HOW TO GET TYPES loaded, this feels a bit heavy handed and relies on Autofac where we can probably do something different
  public SpeckleContainerBuilder LoadAutofacModules(IEnumerable<string> dependencyPaths)
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
            ContainerBuilder.RegisterModule(module);
          }
        }
        // POC: catch only certain exceptions
        catch (Exception ex) when (!ex.IsFatal()) { }
      }
    }

    return this;
  }

  private readonly Lazy<IReadOnlyList<Type>> _types =
    new(() =>
    {
      var types = new List<Type>();
      foreach (
        var asm in AppDomain.CurrentDomain
          .GetAssemblies()
          .Where(x => x.GetName().Name.StartsWith("Speckle", StringComparison.OrdinalIgnoreCase))
      )
      {
        types.AddRange(asm.GetTypes());
      }

      return types;
    });

  public IReadOnlyList<Type> SpeckleTypes => _types.Value;
  public ContainerBuilder ContainerBuilder { get; }

  public SpeckleContainerBuilder AddModule(Module module)
  {
    ContainerBuilder.RegisterModule(module);

    return this;
  }

  public SpeckleContainerBuilder AddSingletonInstance<T>(T instance)
    where T : class
  {
    ContainerBuilder.RegisterInstance(instance);

    return this;
  }

  public SpeckleContainer Build()
  {
    var container = ContainerBuilder.Build();

    // POC: we could create the factory on construction of the container and then inject that and store it
    var logger = container.Resolve<ILoggerFactory>().CreateLogger<SpeckleContainerBuilder>();

    // POC: we could probably expand on this
    List<string> assemblies = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.FullName).ToList();
    logger.LogInformation("Loaded assemblies: {@Assemblies}", assemblies);

    return new SpeckleContainer(container);
  }
}
