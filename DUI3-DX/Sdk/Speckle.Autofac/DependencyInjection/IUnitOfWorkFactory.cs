namespace Speckle.Autofac.DependencyInjection;

public interface IUnitOfWorkFactory<out TService>
  where TService : class
{
  IUnitOfWork<TService> Resolve();
}
