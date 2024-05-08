namespace Speckle.Autofac.DependencyInjection;

public interface IUnitOfWork<out TService> : IDisposable
  where TService : class
{
  TService Service { get; }
}
