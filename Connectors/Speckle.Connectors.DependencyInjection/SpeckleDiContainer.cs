using System;
using System.Collections.Generic;
using Autofac;

namespace Speckle.Connectors.DependencyInjection;

public class SpeckleDiContainer
{
  private readonly ContainerBuilder _builder;
  private IContainer? _container;

  public SpeckleDiContainer()
  {
    _builder = new ContainerBuilder();
  }

  // **** HOW TO GET TYPES loaded, i.e. kits?
  public SpeckleDiContainer AddDependencies(IEnumerable<string> dependencyPaths, IEnumerable<Type> types)
  {
    return this;
  }

  public SpeckleDiContainer AddInstance<T>(T instance)
    where T : class
  {
    _builder.RegisterInstance(instance);

    return this;
  }

  public SpeckleDiContainer Build()
  {
    _container = _builder.Build();

    return this;
  }

  public T Resolve<T>()
    where T : class
  {
    // POC: resolve null check with a check and throw perhaps?

    return _container.Resolve<T>();
  }
}
