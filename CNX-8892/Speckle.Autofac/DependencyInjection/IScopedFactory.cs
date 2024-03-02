namespace Speckle.Autofac.DependencyInjection;

public interface IScopedFactory<T>
  where T : class
{
  T CreateScopedInstance();
}
