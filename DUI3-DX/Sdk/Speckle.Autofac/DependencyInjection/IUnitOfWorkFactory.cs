namespace Speckle.Autofac.DependencyInjection;

public interface IUnitOfWorkFactory
{
  // POC: this takes a TService but I wonder if the resolution could be in the
  // Resolve method
  IUnitOfWork<TService> Resolve<TService>()
    where TService : class;
}
